import { FluentProvider, webDarkTheme, webLightTheme } from "@fluentui/react-components";
import "./App.css";
import { AppShell } from "./components/AppShell";
import { EnvironmentsLanding } from "./features/environments/EnvironmentsLanding";
import { usePreferredColorScheme } from "./hooks/usePreferredColorScheme";
import { getInitialEnvironmentsLandingState } from "./services/environments";

function App() {
  const colorScheme = usePreferredColorScheme();
  const environmentsState = getInitialEnvironmentsLandingState();
  const theme = colorScheme === "dark" ? webDarkTheme : webLightTheme;

  return (
    <FluentProvider theme={theme} className="app-provider">
      <AppShell>
        <EnvironmentsLanding state={environmentsState} />
      </AppShell>
    </FluentProvider>
  );
}

export default App;
