import { Button, Card, CardHeader, Text, Title3 } from "@fluentui/react-components";
import type { EnvironmentsLandingState } from "../../services/environments";

interface EnvironmentsLandingProps {
  state: EnvironmentsLandingState;
}

export function EnvironmentsLanding({ state }: EnvironmentsLandingProps) {
  return (
    <section className="environment-landing" aria-labelledby="environments-title">
      <Card className="environment-landing__card">
        <CardHeader
          header={
            <Title3 as="h2" id="environments-title">
              Environments
            </Title3>
          }
          description="Connect source and target environments before defining migration scope."
        />
        {renderState(state)}
      </Card>
    </section>
  );
}

function renderState(state: EnvironmentsLandingState) {
  switch (state.kind) {
    case "loading":
      return (
        <div className="environment-landing__state" role="status" aria-live="polite">
          <Text weight="semibold">{state.message}</Text>
          <Text>Environment details are being prepared.</Text>
        </div>
      );
    case "empty":
      return (
        <div className="environment-landing__state">
          <Text weight="semibold">{state.title}</Text>
          <Text>{state.description}</Text>
          <div className="environment-landing__actions">
            <Button appearance="primary">Add environment</Button>
            <Button appearance="secondary">Import connection</Button>
          </div>
        </div>
      );
    case "error":
      return (
        <div className="environment-landing__state" role="alert">
          <Text weight="semibold">{state.title}</Text>
          <Text>{state.message}</Text>
          <Button appearance="primary">Retry</Button>
        </div>
      );
    case "ready":
      return (
        <div className="environment-landing__state">
          <Text weight="semibold">Connected environments</Text>
          {state.environments.map((environment) => (
            <Text key={environment.id}>
              {environment.displayName} · {environment.region} · {environment.status}
            </Text>
          ))}
        </div>
      );
  }
}
