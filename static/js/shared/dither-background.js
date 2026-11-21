const settings = {
    pixelSize: 2,
    waveSpeed: 0.05,
    pointerRadius: 220,
    pointerStrength: 0.45,
    trailFadeSeconds: 0.4,
    opacity: 0.36,
    darkAccentBrightness: 0.82,
};

const noisePeriod = 8;
const noiseSize = 256;
const samplesPerUnit = noiseSize / noisePeriod;

function smoothstep(value) {
    const t = Math.max(0, Math.min(1, value));
    return t * t * (3 - 2 * t);
}

function gradientDot(x, y, dx, dy, period) {
    const hash =
        Math.imul(x % period, 374761393) + Math.imul(y % period, 668265263);
    const direction = Math.imul(hash ^ (hash >>> 13), 1274126177) >>> 30;
    return (direction & 1 ? -dx : dx) + (direction & 2 ? -dy : dy);
}

function gradientNoise(x, y, period) {
    const left = Math.floor(x);
    const top = Math.floor(y);
    const dx = x - left;
    const dy = y - top;
    const blendX = smoothstep(dx);
    const blendY = smoothstep(dy);
    const a = gradientDot(left, top, dx, dy, period);
    const b = gradientDot(left + 1, top, dx - 1, dy, period);
    const c = gradientDot(left, top + 1, dx, dy - 1, period);
    const d = gradientDot(left + 1, top + 1, dx - 1, dy - 1, period);
    const upper = a + (b - a) * blendX;
    const lower = c + (d - c) * blendX;
    return upper + (lower - upper) * blendY;
}

function createNoiseTile() {
    const tile = new Float32Array(noiseSize * noiseSize);
    for (let y = 0; y < noiseSize; y++) {
        for (let x = 0; x < noiseSize; x++) {
            let density = 0;
            let frequency = 1;
            let amplitude = 1.5;
            for (let octave = 0; octave < 4; octave++) {
                density +=
                    amplitude *
                    Math.abs(
                        gradientNoise(
                            (x * frequency) / samplesPerUnit,
                            (y * frequency) / samplesPerUnit,
                            noisePeriod * frequency,
                        ),
                    );
                frequency *= 3;
                amplitude *= 0.3;
            }

            tile[y * noiseSize + x] = density;
        }
    }

    return tile;
}

function sampleNoise(tile, x, y) {
    const left = Math.floor(x);
    const top = Math.floor(y);
    const column = left & (noiseSize - 1);
    const nextColumn = (left + 1) & (noiseSize - 1);
    const row = (top & (noiseSize - 1)) * noiseSize;
    const nextRow = ((top + 1) & (noiseSize - 1)) * noiseSize;
    const dx = x - left;
    const upper =
        tile[row + column] + (tile[row + nextColumn] - tile[row + column]) * dx;
    const lower =
        tile[nextRow + column] +
        (tile[nextRow + nextColumn] - tile[nextRow + column]) * dx;
    return upper + (lower - upper) * (y - top);
}

function bayerDigit(x, y) {
    return (((x ^ y) & 1) << 1) | (x & 1);
}

const thresholds = Float32Array.from({ length: 64 }, (_, index) => {
    const x = index % 8;
    const y = index >> 3;
    const rank =
        16 * bayerDigit(x, y) +
        4 * bayerDigit(x >> 1, y >> 1) +
        bayerDigit(x >> 2, y >> 2);
    return (rank + 0.5) / 64;
});

const canvas = document.createElement("canvas");
const context = canvas.getContext("2d");

if (context) {
    canvas.className = "dither-background";
    canvas.setAttribute("aria-hidden", "true");
    canvas.style.opacity = settings.opacity;
    document.body.prepend(canvas);

    const root = document.documentElement;
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");
    const noise = createNoiseTile();
    const pointer = {
        x: 0,
        y: 0,
        targetX: 0,
        targetY: 0,
        strength: 0,
        active: false,
    };
    const trail = [];
    const accentBytes = new Uint8ClampedArray(8);
    const accentColors = new Uint32Array(accentBytes.buffer);
    let image;
    let dots;
    let erasure;
    let sampleSize;
    let columns;
    let rows;
    let width;
    let height;
    let frame = null;
    let lastTime = null;
    let time = 0;

    function eraseAround(point) {
        const radius = settings.pointerRadius * (0.55 + 0.45 * point.strength);
        const firstX = Math.max(0, Math.floor((point.x - radius) / sampleSize));
        const lastX = Math.min(
            columns,
            Math.ceil((point.x + radius) / sampleSize),
        );
        const firstY = Math.max(0, Math.floor((point.y - radius) / sampleSize));
        const lastY = Math.min(
            rows,
            Math.ceil((point.y + radius) / sampleSize),
        );

        for (let y = firstY; y < lastY; y++) {
            const dy = (y + 0.5) * sampleSize - point.y;
            for (let x = firstX; x < lastX; x++) {
                const dx = (x + 0.5) * sampleSize - point.x;
                const fade =
                    1 - smoothstep(Math.sqrt(dx * dx + dy * dy) / radius);
                const index = y * columns + x;
                erasure[index] = Math.max(
                    erasure[index],
                    fade * point.strength,
                );
            }
        }
    }

    function updateTrail(delta) {
        const follow = 1 - Math.exp(-delta / 0.06);
        pointer.x += (pointer.targetX - pointer.x) * follow;
        pointer.y += (pointer.targetY - pointer.y) * follow;
        const fade = 1 - Math.exp(-delta / (pointer.active ? 0.12 : 0.35));
        pointer.strength += (Number(pointer.active) - pointer.strength) * fade;

        const decay = Math.exp(-delta / settings.trailFadeSeconds);
        for (const point of trail) {
            point.strength *= decay;
        }

        const last = trail.at(-1);
        if (
            pointer.active &&
            pointer.strength >= 0.01 &&
            (!last || Math.hypot(pointer.x - last.x, pointer.y - last.y) >= 18)
        ) {
            trail.push({
                x: pointer.x,
                y: pointer.y,
                strength: pointer.strength,
            });
        }

        while (trail.length >= 12 || trail[0]?.strength < 0.01) {
            trail.shift();
        }

        erasure.fill(0);
        if (pointer.strength >= 0.01) {
            eraseAround(pointer);
        }

        for (const point of trail) {
            eraseAround(point);
        }
    }

    function draw(timestamp) {
        frame = null;
        const delta =
            lastTime === null
                ? 0
                : Math.min((timestamp - lastTime) / 1000, 0.05);
        lastTime = timestamp;
        time = (time + delta * settings.waveSpeed) % noisePeriod;
        updateTrail(delta);

        const scale = (2.3 * samplesPerUnit) / Math.max(height, 540);
        const drift = time * samplesPerUnit;
        const stride = image.width;
        const [accent, darkAccent] = accentColors;

        for (let y = 0; y < rows; y++) {
            const screenY = (y + 0.5) * sampleSize;
            const py = (screenY - height / 2) * scale;
            const dy = screenY / height - 0.5;
            const matrixRow = (y % 4) * 16;
            for (let x = 0; x < columns; x++) {
                const screenX = (x + 0.5) * sampleSize;
                const px = (screenX - width / 2) * scale;
                const warp = sampleNoise(noise, px - drift, py - drift);
                const offset = warp * samplesPerUnit;
                const density =
                    sampleNoise(noise, px + offset, py + offset) -
                    erasure[y * columns + x] * settings.pointerStrength;
                const dx = screenX / width - 0.5;
                const vignette =
                    1 -
                    0.32 *
                        smoothstep(
                            (Math.sqrt(dx * dx + dy * dy) - 0.28) / 0.44,
                        );
                const coverage =
                    Math.min(0.78, (density - 0.24) * 1.35) * vignette;
                const color = warp > 0.35 ? darkAccent : accent;

                // Share one noise sample across four dots, each with its own Bayer threshold.
                const tile = matrixRow + (x % 4) * 2;
                const dot = (y * stride + x) * 2;
                dots[dot] = coverage > thresholds[tile] ? color : 0;
                dots[dot + 1] = coverage > thresholds[tile + 1] ? color : 0;
                dots[dot + stride] =
                    coverage > thresholds[tile + 8] ? color : 0;
                dots[dot + stride + 1] =
                    coverage > thresholds[tile + 9] ? color : 0;
            }
        }

        context.putImageData(image, 0, 0);
        if (!reducedMotion.matches) {
            requestDraw();
        }
    }

    function requestDraw() {
        if (frame === null && !document.hidden) {
            frame = requestAnimationFrame(draw);
        }
    }

    function resetAnimation() {
        if (frame !== null) {
            cancelAnimationFrame(frame);
            frame = null;
        }

        lastTime = null;
        pointer.active = false;
        pointer.strength = 0;
        trail.length = 0;
        requestDraw();
    }

    function resize() {
        width = Math.max(1, root.clientWidth);
        height = Math.max(1, root.clientHeight);
        const size = Math.max(
            settings.pixelSize,
            Math.ceil(Math.sqrt((width * height) / 900000)),
        );
        const ratio = window.devicePixelRatio;
        const pixelSize = Math.max(1, Math.round(size * ratio)) / ratio;
        sampleSize = 2 * pixelSize;
        columns = Math.ceil(width / sampleSize);
        rows = Math.ceil(height / sampleSize);

        canvas.width = columns * 2;
        canvas.height = rows * 2;
        canvas.style.width = `${canvas.width * pixelSize}px`;
        canvas.style.height = `${canvas.height * pixelSize}px`;
        image = context.createImageData(canvas.width, canvas.height);
        dots = new Uint32Array(image.data.buffer);
        erasure = new Float32Array(columns * rows);
        requestDraw();
    }

    function updateAccent() {
        const color = getComputedStyle(root)
            .getPropertyValue("--dm-accent")
            .trim();
        const rgb = Number.parseInt(color.slice(1), 16);
        const channels = [rgb >> 16, (rgb >> 8) & 255, rgb & 255];
        accentBytes.set([...channels, 255], 0);
        accentBytes.set(
            [
                ...channels.map(
                    (channel) => channel * settings.darkAccentBrightness,
                ),
                255,
            ],
            4,
        );
        requestDraw();
    }

    function releasePointer() {
        pointer.active = false;
    }

    window.addEventListener(
        "pointermove",
        (event) => {
            if (event.pointerType === "touch" || reducedMotion.matches) {
                return;
            }

            if (!pointer.active && pointer.strength < 0.01) {
                pointer.x = event.clientX;
                pointer.y = event.clientY;
            }

            pointer.targetX = event.clientX;
            pointer.targetY = event.clientY;
            pointer.active = true;
        },
        { passive: true },
    );
    root.addEventListener("pointerleave", releasePointer);
    new MutationObserver(updateAccent).observe(root, {
        attributeFilter: ["style"],
    });
    window.addEventListener("blur", releasePointer);
    window.addEventListener("resize", resize);
    document.addEventListener("visibilitychange", resetAnimation);
    reducedMotion.addEventListener("change", resetAnimation);

    updateAccent();
    resize();
}
