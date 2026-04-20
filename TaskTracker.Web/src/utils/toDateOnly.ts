export function toDateOnly(value: unknown): string {
  if (value == null) {
    return "";
  }

  if (typeof value === "string") {
    const trimmedValue = value.trim();
    if (/^\d{4}-\d{2}-\d{2}$/.test(trimmedValue)) {
      return trimmedValue;
    }
  }

  const date = new Date(value as string | number | Date);
  if (Number.isNaN(date.getTime())) {
    return "";
  }

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}
