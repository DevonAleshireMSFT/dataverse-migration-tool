import { Button, Text, Title2 } from "@fluentui/react-components";
import type { ReactNode } from "react";

interface AppShellProps {
  children: ReactNode;
}

export function AppShell({ children }: AppShellProps) {
  return (
    <div className="app-shell">
      <header className="app-shell__header">
        <div className="app-shell__brand">
          <Title2 as="h1">Dataverse Migration Tool</Title2>
          <Text size={300}>Plan, validate, and monitor Dataverse migrations.</Text>
        </div>
        <Button appearance="secondary">Settings</Button>
      </header>
      <main className="app-shell__main" aria-label="Dataverse Migration Tool workspace">
        {children}
      </main>
    </div>
  );
}
