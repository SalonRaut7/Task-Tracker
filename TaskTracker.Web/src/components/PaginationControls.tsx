import { Button } from "devextreme-react/button";
import SelectBox from "devextreme-react/select-box";

interface PaginationControlsProps {
  page: number;
  pageSize: number;
  totalCount: number;
  loading?: boolean;
  pageSizeOptions?: number[];
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

const defaultPageSizeOptions = [10, 20, 50, 100];

export function PaginationControls({
  page,
  pageSize,
  totalCount,
  loading = false,
  pageSizeOptions = defaultPageSizeOptions,
  onPageChange,
  onPageSizeChange,
}: PaginationControlsProps) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const currentPage = Math.min(page, totalPages - 1);
  const hasRows = totalCount > 0;

  const startItem = hasRows ? currentPage * pageSize + 1 : 0;
  const endItem = hasRows ? Math.min(totalCount, (currentPage + 1) * pageSize) : 0;

  const disablePrevious = loading || currentPage <= 0;
  const disableNext = loading || currentPage >= totalPages - 1;

  return (
    <div className="pagination-controls">
      <div className="pagination-summary">
        <span>
          Showing {startItem}-{endItem} of {totalCount}
        </span>
      </div>

      <div className="pagination-actions">
        <span className="pagination-page-label">
          Page {totalCount === 0 ? 0 : currentPage + 1} of {totalCount === 0 ? 0 : totalPages}
        </span>

        <Button
          text="Prev"
          stylingMode="outlined"
          disabled={disablePrevious}
          onClick={() => onPageChange(currentPage - 1)}
        />

        <Button
          text="Next"
          stylingMode="outlined"
          disabled={disableNext}
          onClick={() => onPageChange(currentPage + 1)}
        />

        <div className="pagination-size">
          <span>Rows</span>
          <SelectBox
            width={84}
            dataSource={pageSizeOptions}
            value={pageSize}
            disabled={loading}
            onValueChanged={(event) => {
              const nextPageSize = Number(event.value ?? pageSize);
              if (Number.isFinite(nextPageSize) && nextPageSize > 0) {
                onPageSizeChange(nextPageSize);
              }
            }}
          />
        </div>
      </div>
    </div>
  );
}
