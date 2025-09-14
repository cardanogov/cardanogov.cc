import { CommonModule } from '@angular/common';
import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { NbCardModule, NbIconModule } from '@nebular/theme';
import { formatValue } from '../../../../core/helper/format.helper';

@Component({
  selector: 'app-introduce-modal',
  imports: [CommonModule, NbIconModule, NbCardModule],
  templateUrl: './introduce-modal.component.html',
  styleUrl: './introduce-modal.component.scss'
})
export class IntroduceModalComponent {
  constructor(
    public dialogRef: MatDialogRef<IntroduceModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {}

  formatValue(value: number): string {
    return formatValue(value);
  }

  close() {
    this.dialogRef.close();
  }
}
