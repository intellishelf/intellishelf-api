import { useState, useEffect } from 'react';

interface HSLColor {
  h: number;
  s: number;
  l: number;
}

const rgbToHsl = (r: number, g: number, b: number): HSLColor => {
  r /= 255;
  g /= 255;
  b /= 255;

  const max = Math.max(r, g, b);
  const min = Math.min(r, g, b);
  let h = 0,
    s = 0,
    l = (max + min) / 2;

  if (max !== min) {
    const d = max - min;
    s = l > 0.5 ? d / (2 - max - min) : d / (max + min);

    switch (max) {
      case r:
        h = ((g - b) / d + (g < b ? 6 : 0)) / 6;
        break;
      case g:
        h = ((b - r) / d + 2) / 6;
        break;
      case b:
        h = ((r - g) / d + 4) / 6;
        break;
    }
  }

  return {
    h: Math.round(h * 360),
    s: Math.round(s * 100),
    l: Math.round(l * 100),
  };
};

/**
 * Hook to extract dominant color from a book cover image
 * Returns HSL color string suitable for use in CSS custom properties
 */
export const useBookCoverColor = (coverUrl?: string | null): string => {
  const [dominantColor, setDominantColor] = useState<string>('240 10% 50%');

  useEffect(() => {
    if (!coverUrl) {
      setDominantColor('240 10% 50%'); // Default neutral color
      return;
    }

    const img = new Image();
    img.crossOrigin = 'Anonymous';
    img.src = coverUrl;

    img.onload = () => {
      const canvas = document.createElement('canvas');
      const ctx = canvas.getContext('2d');
      if (!ctx) return;

      canvas.width = img.width;
      canvas.height = img.height;
      ctx.drawImage(img, 0, 0);

      try {
        const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
        const data = imageData.data;

        let r = 0,
          g = 0,
          b = 0,
          count = 0;

        // Sample every 10th pixel for performance
        for (let i = 0; i < data.length; i += 40) {
          r += data[i];
          g += data[i + 1];
          b += data[i + 2];
          count++;
        }

        r = Math.floor(r / count);
        g = Math.floor(g / count);
        b = Math.floor(b / count);

        const hsl = rgbToHsl(r, g, b);
        setDominantColor(`${hsl.h} ${hsl.s}% ${hsl.l}%`);
      } catch (error) {
        // CORS or other error, use default
        console.warn('Failed to extract cover color:', error);
        setDominantColor('240 10% 50%');
      }
    };

    img.onerror = () => {
      setDominantColor('240 10% 50%');
    };
  }, [coverUrl]);

  return dominantColor;
};
