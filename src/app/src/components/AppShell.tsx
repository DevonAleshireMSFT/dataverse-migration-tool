import {
  Caption1,
  makeStyles,
  mergeClasses,
  shorthands,
  Text,
  Title2,
  tokens,
} from "@fluentui/react-components";
import { useEffect, useRef, type ReactNode } from "react";
import { NavLink, useLocation } from "react-router-dom";
import type { WorkflowSection } from "../services/workflowSections";

interface AppShellProps {
  children: ReactNode;
  sections: WorkflowSection[];
}

const useStyles = makeStyles({
  shell: {
    display: "grid",
    minHeight: "100vh",
    gridTemplateRows: "auto 1fr",
    backgroundColor: tokens.colorNeutralBackground2,
    color: tokens.colorNeutralForeground1,
  },
  skipLink: {
    position: "absolute",
    top: tokens.spacingVerticalM,
    left: "-100vw",
    zIndex: 1,
    backgroundColor: tokens.colorNeutralBackground1,
    color: tokens.colorNeutralForeground1,
    ...shorthands.border("1px", "solid", tokens.colorNeutralStroke1),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    ...shorthands.padding(tokens.spacingVerticalS, tokens.spacingHorizontalM),
    textDecorationLine: "none",
    ":focus": {
      left: tokens.spacingHorizontalM,
    },
  },
  header: {
    display: "flex",
    alignItems: "center",
    justifyContent: "space-between",
    gap: tokens.spacingHorizontalL,
    backgroundColor: tokens.colorNeutralBackground1,
    ...shorthands.borderBottom("1px", "solid", tokens.colorNeutralStroke2),
    ...shorthands.padding(tokens.spacingVerticalL, tokens.spacingHorizontalXXL),
    "@media (max-width: 720px)": {
      alignItems: "flex-start",
      flexDirection: "column",
      ...shorthands.padding(tokens.spacingVerticalL, tokens.spacingHorizontalL),
    },
  },
  brand: {
    display: "grid",
    gap: tokens.spacingVerticalXXS,
  },
  layout: {
    display: "grid",
    gridTemplateColumns: "18rem minmax(0, 1fr)",
    minHeight: 0,
    "@media (max-width: 900px)": {
      gridTemplateColumns: "1fr",
    },
  },
  nav: {
    backgroundColor: tokens.colorNeutralBackground1,
    ...shorthands.borderRight("1px", "solid", tokens.colorNeutralStroke2),
    ...shorthands.padding(tokens.spacingVerticalL, tokens.spacingHorizontalL),
    "@media (max-width: 900px)": {
      ...shorthands.borderRight("0"),
      ...shorthands.borderBottom("1px", "solid", tokens.colorNeutralStroke2),
    },
  },
  navList: {
    display: "grid",
    gap: tokens.spacingVerticalXS,
    listStyleType: "none",
    ...shorthands.margin(0),
    ...shorthands.padding(0),
  },
  navLink: {
    display: "grid",
    gap: tokens.spacingVerticalXXS,
    color: tokens.colorNeutralForeground2,
    textDecorationLine: "none",
    ...shorthands.border("1px", "solid", "transparent"),
    ...shorthands.borderRadius(tokens.borderRadiusMedium),
    ...shorthands.padding(tokens.spacingVerticalS, tokens.spacingHorizontalM),
    ":hover": {
      backgroundColor: tokens.colorNeutralBackground1Hover,
      color: tokens.colorNeutralForeground1,
    },
    ":focus-visible": {
      outlineColor: tokens.colorStrokeFocus2,
      outlineOffset: "2px",
      outlineStyle: "solid",
      outlineWidth: "2px",
    },
  },
  activeNavLink: {
    backgroundColor: tokens.colorBrandBackground2,
    color: tokens.colorNeutralForeground1,
    ...shorthands.borderColor(tokens.colorBrandStroke1),
  },
  navHint: {
    color: tokens.colorNeutralForeground3,
  },
  main: {
    minWidth: 0,
    outlineStyle: "none",
    ...shorthands.padding(tokens.spacingVerticalXXL, tokens.spacingHorizontalXXL),
    "@media (max-width: 720px)": {
      ...shorthands.padding(tokens.spacingVerticalL, tokens.spacingHorizontalL),
    },
  },
  content: {
    maxWidth: "72rem",
  },
});

export function AppShell({ children, sections }: AppShellProps) {
  const styles = useStyles();
  const location = useLocation();
  const mainRef = useRef<HTMLElement>(null);

  useEffect(() => {
    mainRef.current?.focus();
  }, [location.pathname]);

  return (
    <div className={styles.shell}>
      <a className={styles.skipLink} href="#main-content">
        Skip to main content
      </a>
      <header className={styles.header}>
        <div className={styles.brand}>
          <Title2 as="h1">Dataverse Migration Tool</Title2>
          <Text size={300}>Plan, validate, execute, and audit Dataverse migrations.</Text>
        </div>
        <Text size={200}>Government-ready by design; not yet certified.</Text>
      </header>
      <div className={styles.layout}>
        <nav className={styles.nav} aria-label="Major migration workflow sections">
          <ul className={styles.navList}>
            {sections.map((section) => (
              <li key={section.id}>
                <NavLink
                  to={section.route}
                  className={({ isActive }) =>
                    mergeClasses(styles.navLink, isActive && styles.activeNavLink)
                  }
                >
                  <Text weight="semibold">{section.title}</Text>
                  <Caption1 className={styles.navHint}>{section.kicker}</Caption1>
                </NavLink>
              </li>
            ))}
          </ul>
        </nav>
        <main
          id="main-content"
          ref={mainRef}
          tabIndex={-1}
          className={styles.main}
          aria-label="Dataverse Migration Tool workspace"
        >
          <div className={styles.content}>{children}</div>
        </main>
      </div>
    </div>
  );
}
