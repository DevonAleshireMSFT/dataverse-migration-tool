import {
  FluentProvider,
  makeStyles,
  webDarkTheme,
  webLightTheme,
} from "@fluentui/react-components";
import { Navigate, Route, Routes } from "react-router-dom";
import "./App.css";
import { AppShell } from "./components/AppShell";
import { EnvironmentsLanding } from "./features/environments/EnvironmentsLanding";
import { WorkflowPlaceholder } from "./features/workflow/WorkflowPlaceholder";
import { usePreferredColorScheme } from "./hooks/usePreferredColorScheme";
import { getInitialEnvironmentsLandingState } from "./services/environments";
import { getWorkflowSections } from "./services/workflowSections";

const useStyles = makeStyles({
  provider: {
    minHeight: "100vh",
  },
});

function App() {
  const styles = useStyles();
  const colorScheme = usePreferredColorScheme();
  const environmentsState = getInitialEnvironmentsLandingState();
  const theme = colorScheme === "dark" ? webDarkTheme : webLightTheme;
  const workflowSections = getWorkflowSections();
  const environmentsSection = workflowSections.find((section) => section.id === "environments");
  const placeholderSections = workflowSections.filter((section) => section.id !== "environments");

  if (!environmentsSection) {
    throw new Error("The Environments workflow section must be configured.");
  }

  return (
    <FluentProvider theme={theme} className={styles.provider}>
      <AppShell sections={workflowSections}>
        <Routes>
          <Route path="/" element={<Navigate to={environmentsSection.route} replace />} />
          <Route
            path={environmentsSection.route}
            element={
              <EnvironmentsLanding section={environmentsSection} state={environmentsState} />
            }
          />
          {placeholderSections.map((section) => (
            <Route
              key={section.id}
              path={section.route}
              element={<WorkflowPlaceholder section={section} />}
            />
          ))}
          <Route path="*" element={<Navigate to={environmentsSection.route} replace />} />
        </Routes>
      </AppShell>
    </FluentProvider>
  );
}

export default App;
