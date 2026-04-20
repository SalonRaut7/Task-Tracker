export function toDateOnly(value: unknown): string {
  if (!value) {
    return "";
  }

  const date = new Date(value as string | number | Date);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  return date.toISOString().split("T")[0];
}