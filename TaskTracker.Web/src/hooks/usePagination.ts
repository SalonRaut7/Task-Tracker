import { useCallback, useMemo, useState } from "react";

export interface PaginationState {
  page: number;
  pageSize: number;
  skip: number;
  totalPages: number;
}

interface UsePaginationOptions {
  totalCount: number;
  initialPageSize?: number;
}

export function usePagination({
  totalCount,
  initialPageSize = 10,
}: UsePaginationOptions) {
  const [page, setPageState] = useState(0);
  const [pageSize, setPageSizeState] = useState(initialPageSize);

  const totalPages = useMemo(() => {
    if (totalCount <= 0) {
      return 1;
    }

    return Math.ceil(totalCount / pageSize);
  }, [pageSize, totalCount]);

  const safePage = Math.min(page, Math.max(0, totalPages - 1));

  const setPage = useCallback((nextPage: number) => {
    setPageState(Math.max(0, nextPage));
  }, []);

  const setPageSize = useCallback((nextPageSize: number) => {
    const normalizedPageSize = Math.max(1, nextPageSize);
    setPageSizeState(normalizedPageSize);
    setPageState(0);
  }, []);

  const resetPage = useCallback(() => {
    setPageState(0);
  }, []);

  return {
    page: safePage,
    pageSize,
    skip: safePage * pageSize,
    totalPages,
    setPage,
    setPageSize,
    resetPage,
  } satisfies PaginationState & {
    setPage: (page: number) => void;
    setPageSize: (pageSize: number) => void;
    resetPage: () => void;
  };
}
