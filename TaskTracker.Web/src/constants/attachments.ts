/** Shared attachment validation constants used by TasksPage and TaskDetailsPage. */

export const ALLOWED_EXTENSIONS = [
  '.txt', '.md', '.pdf',
  '.doc', '.docx',
  '.xls', '.xlsx',
  '.jpg', '.jpeg', '.png', '.gif',
] as const;

export const ALLOWED_EXTENSIONS_ACCEPT = ALLOWED_EXTENSIONS.join(',');

export const MAX_FILE_SIZE_BYTES = 10 * 1024 * 1024; // 10 MB

export const MAX_ATTACHMENTS_PER_TASK = 10;

export function isAllowedFile(file: File): boolean {
  const name = file.name.toLowerCase();
  return ALLOWED_EXTENSIONS.some((ext) => name.endsWith(ext));
}

export function isFileTooLarge(file: File): boolean {
  return file.size > MAX_FILE_SIZE_BYTES;
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

export function validateFiles(
  files: File[],
  existingCount: number
): { valid: File[]; errors: string[] } {
  const errors: string[] = [];
  const valid: File[] = [];

  const remaining = MAX_ATTACHMENTS_PER_TASK - existingCount;
  if (files.length > remaining) {
    errors.push(
      `You can add at most ${remaining} more attachment(s). ${files.length} selected.`
    );
    return { valid: [], errors };
  }

  for (const file of files) {
    if (isFileTooLarge(file)) {
      errors.push(`"${file.name}" exceeds the 10 MB size limit.`);
    } else if (!isAllowedFile(file)) {
      errors.push(`"${file.name}" has an unsupported file type.`);
    } else {
      valid.push(file);
    }
  }

  return { valid, errors };
}
