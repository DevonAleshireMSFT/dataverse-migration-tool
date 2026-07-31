import { useEffect, useState } from "react";

export type PreferredColorScheme = "light" | "dark";

const darkModeQuery = "(prefers-color-scheme: dark)";

function readPreferredColorScheme(): PreferredColorScheme {
  if (typeof window === "undefined") {
    return "light";
  }

  return window.matchMedia(darkModeQuery).matches ? "dark" : "light";
}

export function usePreferredColorScheme(): PreferredColorScheme {
  const [scheme, setScheme] = useState<PreferredColorScheme>(readPreferredColorScheme);

  useEffect(() => {
    const mediaQuery = window.matchMedia(darkModeQuery);
    const handleChange = (event: MediaQueryListEvent) => {
      setScheme(event.matches ? "dark" : "light");
    };

    mediaQuery.addEventListener("change", handleChange);
    return () => mediaQuery.removeEventListener("change", handleChange);
  }, []);

  return scheme;
}
