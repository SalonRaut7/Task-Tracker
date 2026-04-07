import { useEffect, useMemo, useState } from "react";
import Popup from "devextreme-react/popup";
import Form, {
  GroupItem,
  Item,
  Label,
  RequiredRule,
  StringLengthRule,
  CustomRule,
} from "devextreme-react/form";
import Button from "devextreme-react/button";
import type { TaskDto } from "../../types/task";
import { priorityOptions, statusOptions } from "./taskOptions";
import {
  buildCreateDto,
  buildUpdateDto,
  createEmptyTaskDraft,
  type TaskDraft,
} from "./taskHelpers";
import { isTaskValid, isValidDateRange, validateTask } from "./taskValidation";

type TaskEditPopupProps = {
  visible: boolean;
  mode: "add" | "edit";
  task: TaskDto | null;
  saving: boolean;
  onClose: () => void;
  onSave: (data: TaskDraft) => Promise<void>;
};

export default function TaskEditPopup({
  visible,
  mode,
  task,
  saving,
  onClose,
  onSave,
}: TaskEditPopupProps) {
  const [formData, setFormData] = useState<TaskDraft>(createEmptyTaskDraft());

  useEffect(() => {
    if (visible) {
      if (mode === "edit" && task) {
        setFormData({
          id: task.id,
          title: task.title ?? "",
          description: task.description ?? "",
          status: task.status,
          priority: task.priority,
          startDate: task.startDate ?? null,
          endDate: task.endDate ?? null,
          createdAt: task.createdAt ?? null,
        });
      } else {
        setFormData(createEmptyTaskDraft());
      }
    }
  }, [visible, mode, task]);

  const validationErrors = useMemo(() => {
    const dto =
      mode === "edit" ? buildUpdateDto(formData) : buildCreateDto(formData);
    return validateTask(dto);
  }, [formData, mode]);

  const isFormValid = useMemo(() => {
    const dto =
      mode === "edit" ? buildUpdateDto(formData) : buildCreateDto(formData);
    return isTaskValid(dto);
  }, [formData, mode]);

  const handleSave = async () => {
    if (!isFormValid) return;
    await onSave(formData);
  };

  return (
    <Popup
      visible={visible}
      onHiding={onClose}
      dragEnabled={false}
      hideOnOutsideClick={false}
      showCloseButton={true}
      showTitle={true}
      title={mode === "add" ? "Add Task" : "Edit Task"}
      width={760}
      height="auto"
    >
      <div className="task-popup-content">
        <Form
          formData={formData}
          colCount={2}
          labelLocation="top"
          onFieldDataChanged={(e) => {
            setFormData((prev) => ({
              ...prev,
              [e.dataField as string]: e.value,
            }));
          }}
        >
          <GroupItem colCount={2}>
            <Item dataField="title" editorType="dxTextBox">
              <Label text="Title" />
              <RequiredRule message="Title is required." />
              <StringLengthRule max={100} message="Title cannot exceed 100 characters." />
            </Item>

            <Item
              dataField="status"
              editorType="dxSelectBox"
              editorOptions={{
                items: statusOptions,
                valueExpr: "id",
                displayExpr: "name",
                searchEnabled: false,
              }}
            >
              <Label text="Status" />
              <RequiredRule message="Status is required." />
            </Item>

            <Item
              dataField="description"
              colSpan={2}
              editorType="dxTextArea"
              editorOptions={{
                height: 120,
                maxLength: 500,
              }}
            >
              <Label text="Description" />
              <StringLengthRule max={500} message="Description cannot exceed 500 characters." />
            </Item>

            <Item
              dataField="priority"
              editorType="dxSelectBox"
              editorOptions={{
                items: priorityOptions,
                valueExpr: "id",
                displayExpr: "name",
                searchEnabled: false,
              }}
            >
              <Label text="Priority" />
              <RequiredRule message="Priority is required." />
            </Item>

            <Item
              dataField="startDate"
              editorType="dxDateBox"
              editorOptions={{
                type: "date",
                displayFormat: "yyyy-MM-dd",
                dateSerializationFormat: "yyyy-MM-ddTHH:mm:ss",
                showClearButton: true,
                openOnFieldClick: true,
                useMaskBehavior: true,
                pickerType: "calendar",
              }}
            >
              <Label text="Start Date" />
            </Item>

            <Item
              dataField="endDate"
              editorType="dxDateBox"
              editorOptions={{
                type: "date",
                displayFormat: "yyyy-MM-dd",
                dateSerializationFormat: "yyyy-MM-ddTHH:mm:ss",
                showClearButton: true,
                openOnFieldClick: true,
                useMaskBehavior: true,
                pickerType: "calendar",
              }}
            >
              <Label text="End Date" />
              <CustomRule
                message="Start date must be before or equal to end date."
                validationCallback={() =>
                  isValidDateRange(formData.startDate, formData.endDate)
                }
              />
            </Item>

            {mode === "edit" && (
              <Item
                dataField="createdAt"
                editorType="dxDateBox"
                editorOptions={{
                  type: "date",
                  displayFormat: "yyyy-MM-dd",
                  disabled: true,
                  readOnly: true,
                }}
              >
                <Label text="Created On" />
              </Item>
            )}
          </GroupItem>
        </Form>

        {Object.keys(validationErrors).length > 0 && (
          <div className="task-popup-validation-summary" role="alert">
            {Object.values(validationErrors).map((error, index) => (
              <div key={`${error}-${index}`}>{error}</div>
            ))}
          </div>
        )}

        <div className="task-popup-actions">
          <Button
            text="Cancel"
            type="normal"
            stylingMode="outlined"
            onClick={onClose}
          />
          <Button
            text={saving ? "Saving..." : "Save"}
            type="default"
            stylingMode="contained"
            onClick={handleSave}
            disabled={!isFormValid || saving}
          />
        </div>
      </div>
    </Popup>
  );
}