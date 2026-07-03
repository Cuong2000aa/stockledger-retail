import type { Config } from "tailwindcss";

const config: Config = {
  content: ["./src/**/*.{js,ts,jsx,tsx,mdx}"],
  theme: {
    extend: {
      fontFamily: {
        sans: ["var(--font-inter)", "system-ui", "sans-serif"],
      },
      colors: {
        brand: {
          50: "#fef2f2",
          100: "#fee2e2",
          200: "#fecaca",
          300: "#fca5a5",
          400: "#f87171",
          500: "#e53e3e",
          600: "#c8102e",
          700: "#a00d24",
          800: "#7f0a1c",
          900: "#5c0714",
          950: "#3b040d",
        },
        surface: {
          DEFAULT: "#ffffff",
          muted: "#f8fafc",
          sidebar: "#680a10",
          "sidebar-hover": "#7f0c14",
        },
      },
      boxShadow: {
        card: "0 1px 3px 0 rgb(15 23 42 / 0.06), 0 1px 2px -1px rgb(15 23 42 / 0.06)",
        "card-hover":
          "0 10px 25px -5px rgb(15 23 42 / 0.08), 0 4px 10px -4px rgb(15 23 42 / 0.04)",
        glow: "0 0 0 1px rgb(200 16 46 / 0.1), 0 4px 16px -2px rgb(200 16 46 / 0.14)",
      },
      animation: {
        "fade-in": "fadeIn 0.35s ease-out",
        "slide-up": "slideUp 0.4s ease-out",
        shimmer: "shimmer 1.5s infinite",
      },
      keyframes: {
        fadeIn: {
          from: { opacity: "0" },
          to: { opacity: "1" },
        },
        slideUp: {
          from: { opacity: "0", transform: "translateY(8px)" },
          to: { opacity: "1", transform: "translateY(0)" },
        },
        shimmer: {
          "0%": { backgroundPosition: "-200% 0" },
          "100%": { backgroundPosition: "200% 0" },
        },
      },
    },
  },
  plugins: [],
};

export default config;
