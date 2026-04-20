import { useEffect, type MouseEvent, type ReactNode } from "react";
import { createPortal } from "react-dom";

type ModalProps = {
  visible: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
  width?: number;
  closeOnOverlayClick?: boolean;
};

export function Modal({
  visible,
  title,
  onClose,
  children,
  width = 640,
  closeOnOverlayClick = true,
}: ModalProps) {
  useEffect(() => {
    if (!visible) {
      return;
    }

    const originalOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") {
        onClose();
      }
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      window.removeEventListener("keydown", handleKeyDown);
      document.body.style.overflow = originalOverflow;
    };
  }, [onClose, visible]);

  if (!visible) {
    return null;
  }

  const handleOverlayMouseDown = (event: MouseEvent<HTMLDivElement>) => {
    if (!closeOnOverlayClick) {
      return;
    }

    if (event.target === event.currentTarget) {
      onClose();
    }
  };

  return createPortal(
    <div className="app-modal" onMouseDown={handleOverlayMouseDown}>
      <div className="app-modal-dialog" style={{ width }} role="dialog" aria-modal="true">
        <div className="app-modal-header">
          <div className="app-modal-title">{title}</div>
          <button type="button" className="app-modal-close" onClick={onClose} aria-label="Close dialog">
            x
          </button>
        </div>

        <div className="app-modal-content">{children}</div>
      </div>
    </div>,
    document.body
  );
}
