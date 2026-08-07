import {
  Badge,
  Button,
  Card,
  CardHeader,
  makeStyles,
  shorthands,
  Text,
  Title3,
  tokens,
} from "@fluentui/react-components";
import type { EnvironmentsLandingState } from "../../services/environments";
import type { WorkflowSection } from "../../services/workflowSections";

interface EnvironmentsLandingProps {
  section: WorkflowSection;
  state: EnvironmentsLandingState;
}

const useStyles = makeStyles({
  page: {
    display: "grid",
    gap: tokens.spacingVerticalL,
  },
  card: {
    display: "grid",
    gap: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  state: {
    display: "grid",
    gap: tokens.spacingVerticalS,
    backgroundColor: tokens.colorNeutralBackground2,
    ...shorthands.border("1px", "solid", tokens.colorNeutralStroke2),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingHorizontalM),
  },
  actions: {
    display: "flex",
    flexWrap: "wrap",
    gap: tokens.spacingHorizontalS,
  },
  environmentList: {
    display: "grid",
    gap: tokens.spacingVerticalS,
    listStyleType: "none",
    ...shorthands.margin(0),
    ...shorthands.padding(0),
  },
  environmentItem: {
    display: "flex",
    alignItems: "center",
    justifyContent: "space-between",
    gap: tokens.spacingHorizontalM,
    ...shorthands.border("1px", "solid", tokens.colorNeutralStroke2),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    ...shorthands.padding(tokens.spacingVerticalS, tokens.spacingHorizontalM),
  },
});

export function EnvironmentsLanding({ section, state }: EnvironmentsLandingProps) {
  const styles = useStyles();

  return (
    <section className={styles.page} aria-labelledby="environments-title">
      <Card className={styles.card}>
        <CardHeader
          header={
            <Title3 as="h2" id="environments-title">
              {section.title}
            </Title3>
          }
          description={section.description}
        />
        {renderState(state, styles)}
      </Card>
    </section>
  );
}

function renderState(state: EnvironmentsLandingState, styles: ReturnType<typeof useStyles>) {
  switch (state.kind) {
    case "loading":
      return (
        <div className={styles.state} role="status" aria-live="polite">
          <Text weight="semibold">{state.message}</Text>
          <Text>Environment details are being prepared.</Text>
        </div>
      );
    case "empty":
      return (
        <div className={styles.state}>
          <Text weight="semibold">{state.title}</Text>
          <Text>{state.description}</Text>
          <div className={styles.actions} aria-label="Environment connection actions">
            <Button appearance="primary">Add environment</Button>
            <Button appearance="secondary">Import connection</Button>
          </div>
        </div>
      );
    case "error":
      return (
        <div className={styles.state} role="alert">
          <Text weight="semibold">{state.title}</Text>
          <Text>{state.message}</Text>
          <Button appearance="primary">Retry</Button>
        </div>
      );
    case "ready":
      return (
        <div className={styles.state}>
          <Text weight="semibold">Connected environments</Text>
          <ul className={styles.environmentList} aria-label="Connected Dataverse environments">
            {state.environments.map((environment) => (
              <li className={styles.environmentItem} key={environment.id}>
                <span>
                  <Text weight="semibold">{environment.displayName}</Text>
                  <Text block size={200}>
                    {environment.region}
                  </Text>
                </span>
                <Badge appearance={environment.status === "connected" ? "filled" : "tint"}>
                  {environment.status === "connected" ? "Connected" : "Needs attention"}
                </Badge>
              </li>
            ))}
          </ul>
        </div>
      );
  }
}
