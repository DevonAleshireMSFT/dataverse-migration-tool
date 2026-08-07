import {
  Badge,
  Body1,
  Card,
  CardHeader,
  makeStyles,
  shorthands,
  Text,
  Title3,
  tokens,
} from "@fluentui/react-components";
import type { WorkflowSection } from "../../services/workflowSections";

interface WorkflowPlaceholderProps {
  section: WorkflowSection;
}

const useStyles = makeStyles({
  page: {
    display: "grid",
    gap: tokens.spacingVerticalL,
  },
  introCard: {
    display: "grid",
    gap: tokens.spacingVerticalM,
    backgroundColor: tokens.colorNeutralBackground1,
  },
  body: {
    display: "grid",
    gap: tokens.spacingVerticalM,
    ...shorthands.padding(0, tokens.spacingHorizontalL, tokens.spacingVerticalL),
  },
  capabilityList: {
    display: "grid",
    gap: tokens.spacingVerticalS,
    ...shorthands.margin(0),
    ...shorthands.padding(0, 0, 0, tokens.spacingHorizontalL),
  },
  callout: {
    display: "grid",
    gap: tokens.spacingVerticalXS,
    backgroundColor: tokens.colorNeutralBackground2,
    ...shorthands.border("1px", "solid", tokens.colorNeutralStroke2),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    ...shorthands.padding(tokens.spacingVerticalM, tokens.spacingHorizontalM),
  },
});

export function WorkflowPlaceholder({ section }: WorkflowPlaceholderProps) {
  const styles = useStyles();

  return (
    <section className={styles.page} aria-labelledby={`${section.id}-title`}>
      <Card className={styles.introCard}>
        <CardHeader
          header={
            <Title3 as="h2" id={`${section.id}-title`}>
              {section.title}
            </Title3>
          }
          description={section.description}
          action={<Badge appearance="tint">Planned</Badge>}
        />
        <div className={styles.body}>
          <Body1>{section.operatorOutcome}</Body1>
          <div
            className={styles.callout}
            role="note"
            aria-label={`${section.title} implementation status`}
          >
            <Text weight="semibold">Implementation placeholder</Text>
            <Text>
              This route is wired into the operator shell. Backend-backed workflows will land behind
              typed application service contracts in later feature issues.
            </Text>
          </div>
          <Text weight="semibold">Upcoming capabilities</Text>
          <ul className={styles.capabilityList}>
            {section.upcomingCapabilities.map((capability) => (
              <li key={capability}>
                <Text>{capability}</Text>
              </li>
            ))}
          </ul>
        </div>
      </Card>
    </section>
  );
}
