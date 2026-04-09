import DataGrid, {
  ColumnChooser,
  ColumnFixing,
  Column,
  Export,
  FilterRow,
  HeaderFilter,
  LoadPanel,
  Pager,
  Paging,
  SearchPanel,
  Selection,
  Toolbar,
  Item as ToolbarItem,
} from "devextreme-react/data-grid";
import Button from "devextreme-react/button";
import CustomStore from "devextreme/data/custom_store";
import type { ExportingEvent } from "devextreme/ui/data_grid";
import type { TaskDto } from "../../types/task";
import { priorityOptions, statusOptions } from "./taskOptions";
import { PriorityBadge, StatusBadge } from "./TaskBadges";

type TaskGridProps = {
  tasks: CustomStore<TaskDto, number>;
  onAdd: () => void;
  onEdit: (task: TaskDto) => void;
  onDelete: (task: TaskDto) => void;
  onExporting: (e: ExportingEvent<TaskDto, number>) => void | Promise<void>;
};

export default function TaskGrid({
  tasks,
  onAdd,
  onEdit,
  onDelete,
  onExporting,
}: TaskGridProps) {
  return (
    <DataGrid<TaskDto, number>
      dataSource={tasks}
      keyExpr="id"
      showBorders={false}
      columnAutoWidth={true}
      rowAlternationEnabled={true}
      wordWrapEnabled={true}
      hoverStateEnabled={true}
      repaintChangesOnly={true}
      allowColumnReordering={true}
      allowColumnResizing={true}
      remoteOperations={{ paging: true, sorting: false, filtering: false }}
      columnResizingMode="widget"
      height={700}
      noDataText="No tasks found."
      onExporting={onExporting}
    >
      <LoadPanel enabled={true} />
      <SearchPanel visible={true} placeholder="Search tasks..." width={260} />
      <FilterRow visible={true} />
      <HeaderFilter visible={true} />
      <Selection
        mode="multiple"
        showCheckBoxesMode="always"
        allowSelectAll={true}
        selectAllMode="page"
      />
      <ColumnChooser enabled={true} mode="select" />
      <ColumnFixing enabled={true} />
      <Paging defaultPageSize={10} />
      <Pager
        showPageSizeSelector={true}
        allowedPageSizes={[5, 10, 20, 50]}
        showInfo={true}
        showNavigationButtons={true}
      />
      <Export
        enabled={true}
        formats={["xlsx", "pdf"]}
        allowExportSelectedData={true}
      />

      <Toolbar>
        <ToolbarItem location="before">
          <Button
            text="Add Task"
            type="default"
            stylingMode="contained"
            onClick={onAdd}
            icon="add"
          />
        </ToolbarItem>
        <ToolbarItem name="searchPanel" />
        <ToolbarItem name="columnChooserButton" />
        <ToolbarItem name="exportButton" />
      </Toolbar>

      <Column
        dataField="title"
        caption="Title"
        minWidth={180}
        allowSearch={true}
        allowFiltering={true}
        allowHiding={false}
      />

      <Column
        dataField="description"
        caption="Description"
        minWidth={220}
        visible={true}
        allowSearch={true}
        allowFiltering={true}
        allowHiding={true}
      />

      <Column
        dataField="status"
        caption="Status"
        minWidth={140}
        allowSearch={true}
        allowFiltering={true}
        lookup={{
          dataSource: statusOptions,
          valueExpr: "id",
          displayExpr: "name",
        }}
        cellRender={(cell) => <StatusBadge value={cell.value} />}
      />

      <Column
        dataField="priority"
        caption="Priority"
        minWidth={140}
        allowSearch={true}
        allowFiltering={true}
        lookup={{
          dataSource: priorityOptions,
          valueExpr: "id",
          displayExpr: "name",
        }}
        cellRender={(cell) => <PriorityBadge value={cell.value} />}
      />

      <Column
        dataField="startDate"
        caption="Start Date"
        dataType="date"
        format="yyyy-MM-dd"
        minWidth={130}
        allowSearch={false}
        allowFiltering={true}
      />

      <Column
        dataField="endDate"
        caption="End Date"
        dataType="date"
        format="yyyy-MM-dd"
        minWidth={130}
        allowSearch={false}
        allowFiltering={true}
      />

      <Column
        dataField="createdAt"
        caption="Created On"
        dataType="date"
        format="yyyy-MM-dd"
        allowEditing={false}
        minWidth={130}
        allowSearch={false}
        allowFiltering={true}
      />

      <Column
        caption="Actions"
        width={180}
        allowExporting={false}
        allowFiltering={false}
        allowSorting={false}
        allowSearch={false}
        allowHiding={false}
        cellRender={({ data }) => (
          <div className="task-grid-actions">
            <Button
              icon="edit"
              stylingMode="outlined"
              onClick={() => onEdit(data)}
            />
            <Button
              icon="trash"
              type="danger"
              stylingMode="outlined"
              onClick={() => onDelete(data)}
            />
          </div>
        )}
      />
    </DataGrid>
  );
}