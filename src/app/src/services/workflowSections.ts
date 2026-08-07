export type WorkflowSectionId =
  | "environments"
  | "metadata-discovery"
  | "compare-readiness"
  | "validation"
  | "migration-jobs"
  | "settings-about";

export interface WorkflowSection {
  id: WorkflowSectionId;
  title: string;
  route: string;
  kicker: string;
  description: string;
  operatorOutcome: string;
  upcomingCapabilities: string[];
}

const workflowSections: WorkflowSection[] = [
  {
    id: "environments",
    title: "Environments & Connections",
    route: "/environments",
    kicker: "Source and target setup",
    description:
      "Connect source and target Dataverse environments before defining migration scope.",
    operatorOutcome:
      "Operators will verify cloud, tenant, and connection readiness without exposing secrets in browser state.",
    upcomingCapabilities: [
      "Add environment profiles",
      "Verify connectivity",
      "Select source and target roles",
    ],
  },
  {
    id: "metadata-discovery",
    title: "Metadata Discovery",
    route: "/metadata-discovery",
    kicker: "Tables, relationships, and solution components",
    description:
      "Discover Dataverse metadata and solution components available for migration planning.",
    operatorOutcome:
      "Operators will see supported tables, dependencies, relationships, and component inventory before scoping a run.",
    upcomingCapabilities: ["Metadata inventory", "Dependency graph", "Scope recommendations"],
  },
  {
    id: "compare-readiness",
    title: "Compare & Readiness",
    route: "/compare-readiness",
    kicker: "Source-to-target differences",
    description:
      "Compare source and target environments to identify readiness gaps before validation.",
    operatorOutcome:
      "Operators will understand environment differences early enough to correct schema, solution, or permission drift.",
    upcomingCapabilities: ["Environment comparison", "Readiness summary", "Blocking issue triage"],
  },
  {
    id: "validation",
    title: "Validation",
    route: "/validation",
    kicker: "Pre-run and post-run confidence",
    description:
      "Run validation checks and review actionable findings before and after migration jobs.",
    operatorOutcome:
      "Operators will get validation reports that separate blockers, warnings, retryable failures, and audit evidence.",
    upcomingCapabilities: ["Pre-run checks", "Post-run reconciliation", "Validation report export"],
  },
  {
    id: "migration-jobs",
    title: "Migration Jobs",
    route: "/migration-jobs",
    kicker: "Durable execution and monitoring",
    description: "Create, monitor, pause, resume, and audit full or incremental migration jobs.",
    operatorOutcome:
      "Operators will track durable server-side jobs without running bulk migration work in the browser session.",
    upcomingCapabilities: ["Create migration job", "Monitor progress", "Resume or cancel safely"],
  },
  {
    id: "settings-about",
    title: "Settings & About",
    route: "/settings-about",
    kicker: "Cloud posture and product information",
    description:
      "Review product posture, supported cloud configuration, and operator-facing settings.",
    operatorOutcome:
      "Operators will understand the tool's government-ready posture, configuration boundaries, and support status.",
    upcomingCapabilities: [
      "Cloud configuration",
      "Version and support details",
      "Audit and privacy notes",
    ],
  },
];

export function getWorkflowSections(): WorkflowSection[] {
  return workflowSections;
}
