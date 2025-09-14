import { CommonModule } from '@angular/common';
import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { NbIconModule } from '@nebular/theme';
@Component({
  selector: 'app-text-modal',
  standalone: true,
  imports: [CommonModule, NbIconModule],
  template: `
    <div class="chart-modal">
      <div class="modal-header">
        <h2>{{ data.title }}</h2>
        <button class="close-btn" (click)="close()">
          <nb-icon icon="close"></nb-icon>
        </button>
      </div>
      <div
        class="chart-container"
        style="background: white; color: #eee; min-height: 80vh;"
      >
        <ng-container *ngIf="data.dynamic">
          <ng-container *ngIf="hasKeys(data.costModels); else noDataBlock">
            <div *ngFor="let key of objectKeys(data.costModels)" class="mb-4">
              <span class="json-string" style="color:#FF6B82;font-weight:bold;"
                >"{{ key }}"</span
              >
              <span
                class="flex flex-wrap"
                style="display:flex;flex-wrap:wrap;align-items:flex-start;"
              >
                <span class="json-mark" style="color:#F28CB1;">[</span>
                <ng-container
                  *ngFor="let num of data.costModels[key]; let i = index"
                >
                  <span class="json-number" style="color:#000000;">{{
                    num
                  }}</span>
                  <span
                    *ngIf="i < data.costModels[key].length - 1"
                    class="json-mark"
                    style="color:#000000;"
                    >,</span
                  >
                </ng-container>
                <span class="json-mark" style="color:#F28CB1;">]</span>
              </span>
            </div>
          </ng-container>
          <ng-template #noDataBlock>
            <div style="color: #ffb4b4; padding: 2rem; text-align: center;">
              No cost model data available.
            </div>
          </ng-template>
        </ng-container>
        <ng-container *ngIf="!data.dynamic">
          <pre
            style="white-space: pre-wrap; word-break: break-all; margin: 0; padding: 1rem; font-size: 1rem; background: #fff; color: #222;"
            >{{ data.content }}</pre
          >
        </ng-container>
      </div>
    </div>
  `,
  styleUrls: ['./modal.component.scss'],
})
export class TextModalComponent {
  constructor(
    public dialogRef: MatDialogRef<TextModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {

  }

  objectKeys(obj: any): string[] {
    return obj && typeof obj === 'object' ? Object.keys(obj) : [];
  }

  hasKeys(obj: any): boolean {
    return obj && typeof obj === 'object' && Object.keys(obj).length > 0;
  }

  formatJson(val: any): string {
    return Array.isArray(val)
      ? '[\n  ' + val.join(',\n  ') + '\n]'
      : JSON.stringify(val, null, 2);
  }

  close() {
    this.dialogRef.close();
  }
}
