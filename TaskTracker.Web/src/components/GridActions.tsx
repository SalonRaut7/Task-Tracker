import type { ReactNode } from "react";

type GridActionsProps = {
  children: ReactNode;
};

export function GridActions({ children }: GridActionsProps) {
  return (
    <div
      className="inline-actions"
      data-grid-actions
      onClick={(e) => e.stopPropagation()}
      onMouseDown={(e) => e.stopPropagation()}
      onPointerDown={(e) => e.stopPropagation()}
    >
      {children}
    </div>
  );
}