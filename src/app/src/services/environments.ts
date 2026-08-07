export interface EnvironmentSummary {
  id: string;
  displayName: string;
  region: string;
  status: "connected" | "needsAttention";
}

export type EnvironmentsLandingState =
  | {
      kind: "loading";
      message: string;
    }
  | {
      kind: "empty";
      title: string;
      description: string;
    }
  | {
      kind: "error";
      title: string;
      message: string;
    }
  | {
      kind: "ready";
      environments: EnvironmentSummary[];
    };

export function getInitialEnvironmentsLandingState(): EnvironmentsLandingState {
  return {
    kind: "empty",
    title: "No environments connected yet",
    description:
      "Start by adding a source and target environment. Connection details will use supported Power Apps APIs through application service contracts as the data layer is introduced.",
  };
}
