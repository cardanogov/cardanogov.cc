import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chart.component.html',
  styleUrl: './chart.component.scss',
})
export class ChartComponent {
  @Input() title: string | undefined;
  @Input() showGroup: boolean | null = null;
  @Input() showToggle: boolean = false;
  @Input() toggleIcon: string = '../../../assets/icons/menu.svg';
  @Input() toggleIconAlt: string = '../../../assets/icons/menu.svg';
  @Input() toggleState: boolean = false;
  @Output() expand = new EventEmitter<void>();
  @Output() group = new EventEmitter<void>();
  @Output() toggle = new EventEmitter<void>();

  groupImage: string = '../../../assets/icons/group.png';

  onExpand() {
    this.expand.emit();
  }

  onGroup() {
    this.group.emit();
    this.groupImage = this.showGroup
      ? '../../../assets/icons/list.png'
      : '../../../assets/icons/group.png';
  }

  onToggle() {
    this.toggle.emit();
  }

  getCurrentToggleIcon(): string {
    return this.toggleState ? this.toggleIconAlt : this.toggleIcon;
  }
}
