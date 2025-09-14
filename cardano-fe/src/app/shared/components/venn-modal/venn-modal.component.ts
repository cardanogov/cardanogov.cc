import { Component, Inject, Input } from '@angular/core';
import { NgbActiveModal } from '@ng-bootstrap/ng-bootstrap';
import {
  VennSet,
  VennDiagramConfig,
  VennDiagramComponent,
} from '../venn-diagram/venn-diagram.component';
import { MatDialogModule } from '@angular/material/dialog';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { NbIconModule } from '@nebular/theme';

@Component({
  selector: 'app-venn-modal',
  templateUrl: './venn-modal.component.html',
  styleUrls: ['./venn-modal.component.scss'],
  standalone: true,
  imports: [MatDialogModule, VennDiagramComponent, NbIconModule],
})
export class VennModalComponent {
  constructor(
    public dialogRef: MatDialogRef<VennModalComponent>,
    @Inject(MAT_DIALOG_DATA)
    public data: { sets: VennSet[]}
  ) {}

  onClose(): void {
    this.dialogRef.close();
  }
}
