export const generateRandomColorCode = (function () {
  const usedColors = new Set(); // Store used color codes

  function generate() {
    let color;
    do {
      // Generate a random hue (0-360)
      const hue = Math.floor(Math.random() * 360);

      // Use high saturation (80-100%) and high lightness (50-70%) for bright colors
      const saturation = Math.floor(Math.random() * 20) + 80; // 80-100%
      const lightness = Math.floor(Math.random() * 20) + 50;  // 50-70%

      // Convert HSL to RGB
      const h = hue / 360;
      const s = saturation / 100;
      const l = lightness / 100;

      let r, g, b;

      if (s === 0) {
        r = g = b = l; // achromatic
      } else {
        const hue2rgb = function hue2rgb(p: number, q: number, t: number) {
          if (t < 0) t += 1;
          if (t > 1) t -= 1;
          if (t < 1/6) return p + (q - p) * 6 * t;
          if (t < 1/2) return q;
          if (t < 2/3) return p + (q - p) * (2/3 - t) * 6;
          return p;
        };

        const q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        const p = 2 * l - q;

        r = hue2rgb(p, q, h + 1/3);
        g = hue2rgb(p, q, h);
        b = hue2rgb(p, q, h - 1/3);
      }

      // Convert to hex
      const toHex = (x: number) => {
        const hex = Math.round(x * 255).toString(16);
        return hex.length === 1 ? '0' + hex : hex;
      };

      color = `#${toHex(r)}${toHex(g)}${toHex(b)}`;
    } while (usedColors.has(color)); // Keep generating until unique

    usedColors.add(color); // Store the new color
    return color;
  }

  // Optional: Expose a reset method to clear used colors
  generate.reset = function () {
    usedColors.clear();
  };

  // Optional: Expose a method to check if a color has been used
  generate.hasColor = function (color: string) {
    return usedColors.has(color);
  };

  return generate;
})();

