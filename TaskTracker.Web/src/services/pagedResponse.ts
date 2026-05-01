export interface PagingOptions {
  skip?: number;
  take?: number;
}

export interface PagedResponse<TItem> {
  data: TItem[];
  totalCount: number;
}

export function appendPagingParams(
  params: URLSearchParams,
  options?: PagingOptions
): void {
  if (typeof options?.skip === "number") {
    params.set("Skip", options.skip.toString());
  }

  if (typeof options?.take === "number") {
    params.set("Take", options.take.toString());
  }
}

export function normalizePagedResponse<TItem>(
  raw: unknown,
  normalizeItem: (item: unknown) => TItem | null
): PagedResponse<TItem> {
  if (Array.isArray(raw)) {
    const data = raw
      .map((item) => normalizeItem(item))
      .filter((item): item is TItem => item !== null);

    return {
      data,
      totalCount: data.length,
    };
  }

  if (!raw || typeof raw !== "object") {
    return { data: [], totalCount: 0 };
  }

  const payload = raw as Record<string, unknown>;
  const rawData = Array.isArray(payload.data)
    ? payload.data
    : Array.isArray(payload.Data)
    ? payload.Data
    : [];

  const data = rawData
    .map((item) => normalizeItem(item))
    .filter((item): item is TItem => item !== null);

  const rawTotalCount = payload.totalCount ?? payload.TotalCount;
  const totalCount =
    typeof rawTotalCount === "number"
      ? rawTotalCount
      : Number.parseInt(String(rawTotalCount ?? data.length), 10) || 0;

  return {
    data,
    totalCount,
  };
}
