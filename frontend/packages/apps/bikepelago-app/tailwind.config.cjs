/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
    },
  },
  plugins: [require('daisyui')],
  daisyui: {
    themes: [
      {
        bikeapelago: {
          "primary": "var(--color-primary-hex)",
          "secondary": "#fbbf24",
          "accent": "#f59e0b",
          "neutral": "#1f2937",
          "base-100": "var(--color-surface-hex)",
          "info": "#3abff8",
          "success": "#36d399",
          "warning": "#fbbd23",
          "error": "#f87272",
        },
      },
      "dark",
    ],
  },
}
