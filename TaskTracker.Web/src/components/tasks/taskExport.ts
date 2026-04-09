import { exportDataGrid as exportDataGridToExcel } from "devextreme/excel_exporter";
import { exportDataGrid as exportDataGridToPdf } from "devextreme/pdf_exporter";
import { Workbook } from "exceljs";
import { saveAs } from "file-saver";
import { jsPDF } from "jspdf";
import type { ExportingEvent } from "devextreme/ui/data_grid";
import type { TaskDto } from "../../types/task";
import { getPriorityText, getStatusText } from "./taskHelpers";

export const exportTasks = async (
  e: ExportingEvent<TaskDto, number>,
  selectedRowsOnly = false
) => {
  if (e.format === "xlsx") {
    const workbook = new Workbook();
    const worksheet = workbook.addWorksheet("Tasks");

    await exportDataGridToExcel({
      component: e.component,
      worksheet,
      autoFilterEnabled: true,
      selectedRowsOnly,
      customizeCell: ({ gridCell, excelCell }) => {
        if (gridCell?.rowType !== "data" || !gridCell.column) return;

        if (gridCell.column.dataField === "status") {
          excelCell.value = getStatusText(gridCell.value as number);
        }

        if (gridCell.column.dataField === "priority") {
          excelCell.value = getPriorityText(gridCell.value as number);
        }
      },
    });

    const buffer = await workbook.xlsx.writeBuffer();
    saveAs(
      new Blob([buffer], { type: "application/octet-stream" }),
      selectedRowsOnly ? "SelectedTasks.xlsx" : "Tasks.xlsx"
    );
  }

  if (e.format === "pdf") {
    const doc = new jsPDF();

    await exportDataGridToPdf({
      jsPDFDocument: doc,
      component: e.component,
      selectedRowsOnly,
      customizeCell: ({ gridCell, pdfCell }) => {
        if (gridCell?.rowType !== "data" || !gridCell.column || !pdfCell) return;

        if (gridCell.column.dataField === "status") {
          pdfCell.text = getStatusText(gridCell.value as number);
        }

        if (gridCell.column.dataField === "priority") {
          pdfCell.text = getPriorityText(gridCell.value as number);
        }
      },
    });

    doc.save(selectedRowsOnly ? "SelectedTasks.pdf" : "Tasks.pdf");
  }
};