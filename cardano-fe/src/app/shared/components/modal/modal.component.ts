import { CommonModule } from '@angular/common';
import {
  Component,
  ElementRef,
  Inject,
  ViewChild,
  AfterViewInit,
} from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { NbIconModule } from '@nebular/theme';
import { Chart, ChartConfiguration } from 'chart.js';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [NbIconModule, CommonModule],
  templateUrl: './modal.component.html',
  styleUrl: './modal.component.scss',
})
export class ModalComponent implements AfterViewInit {
  @ViewChild('expandedChart') expandedChart!: ElementRef;
  chartInstance!: Chart;

  constructor(
    public dialogRef: MatDialogRef<ModalComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {}

  ngAfterViewInit() {
    if (!this.data.originalChart) {
      console.error('Original chart data not provided');
      return;
    }

    const originalConfig = this.data.originalChart.config;

    if(this.data.chartKey === 'technicalGroup') {

      originalConfig.data.datasets[3].color = 'green';
      originalConfig.data.datasets[3].borderColor = 'green';
      originalConfig.data.datasets[3].backgroundColor = 'green';
    }

    // Create a deep copy of the data object manually
    const clonedData = this.deepCopyWithFunctions(originalConfig.data);

    // Try to use a red-yellow-green gradient for the first segment and gray for the second
    if (this.data.chartKey === 'votingParticipation') {
      const canvas = this.expandedChart.nativeElement as HTMLCanvasElement;
      const ctx = canvas.getContext('2d');
      const chartWidth = canvas.getBoundingClientRect().width || 400;
      const gradientSegment = ctx?.createLinearGradient(0, 0, chartWidth, 0);
      gradientSegment?.addColorStop(0, 'red');
      gradientSegment?.addColorStop(0.7, 'yellow');
      gradientSegment?.addColorStop(1, 'green');
      clonedData.datasets.forEach((ds: any) => {
        ds.backgroundColor = [gradientSegment, 'rgba(0, 0, 0, 0.2)'];
        ds.borderColor = [gradientSegment, 'rgba(0, 0, 0, 0.2)'];
        ds.hoverBackgroundColor = [gradientSegment, 'rgba(0, 0, 0, 0.2)'];
        ds.hoverBorderColor = [gradientSegment, 'rgba(0, 0, 0, 0.2)'];
      });
    }

    // Create a new configuration
    const config: ChartConfiguration = {
      type: this.data.type,
      data: clonedData,
      options: this.deepCopyWithFunctions(originalConfig.options),
    };

    // Handle plugins correctly
    if (Array.isArray(originalConfig.plugins)) {
      config.plugins = this.deepCopyWithFunctions(originalConfig.plugins);
    } else if (originalConfig.plugins) {
      config.plugins = this.deepCopyWithFunctions(originalConfig.plugins);
    }

    // Add modal-specific overrides
    if (config.options) {
      config.options.responsive = true;
      config.options.maintainAspectRatio = false;

      // Add tooltip configuration
      // Nếu config gốc có tooltip.callbacks, giữ nguyên, chỉ bổ sung các thuộc tính khác
      const originalTooltip = config.options.plugins?.tooltip || {};
      config.options.plugins = {
        ...config.options.plugins,
        tooltip: {
          ...originalTooltip,
          position: 'nearest',
          intersect: false,
          padding: 10,
          displayColors: false
        }
      };

      // Safely set layout padding
      if (!config.options.layout) {
        config.options.layout = {};
      }
      // For votingParticipation, increase bottom padding for value text
      if (this.data.chartKey === 'votingParticipation') {
        config.options.layout.padding = {
          top: 20,
          right: 20,
          bottom: 120,
          left: 20,
        };
      } else {
        // Check if padding exists and what type it is
        if (!config.options.layout.padding) {
          // If padding doesn't exist, create it as an object
          config.options.layout.padding = {
            top: 20,
            right: 20,
            bottom: 20,
            left: 20,
          };
        } else if (typeof config.options.layout.padding === 'number') {
          // If padding is a number, keep it as a number but make it larger for the modal
          // Chart.js accepts padding as either a number (for all sides) or an object
          config.options.layout.padding = 20;
        } else {
          // If padding is already an object, update its properties
          const padding = config.options.layout.padding as any;
          padding.top = padding.top || 20;
          padding.right = padding.right || 20;
          padding.bottom = padding.bottom || 20;
          padding.left = padding.left || 20;
        }
      }
    }

    // Create the new chart
    this.chartInstance = new Chart(this.expandedChart.nativeElement, config);

    // Nếu là votingParticipation, set lại width/height canvas đúng bằng hình tròn
    if (this.data.chartKey === 'votingParticipation') {
      setTimeout(() => {
        const chart = this.chartInstance;
        const meta = chart.getDatasetMeta(0);
        if (meta && meta.data && meta.data[0]) {
          const arc: any = meta.data[0];
          let outerRadius = arc.outerRadius;
          if (!outerRadius && arc.$context && arc.$context.outerRadius) {
            outerRadius = arc.$context.outerRadius;
          }
          if (outerRadius) {
            const canvas = this.expandedChart.nativeElement as HTMLCanvasElement;
            canvas.width = outerRadius * 2;
            canvas.height = outerRadius * 2;
            chart.resize();
          }
        }
      }, 100);
    }
  }

  /**
   * Safely deep copies objects, arrays, and preserves functions
   * This is a safer alternative to structuredClone() which doesn't handle functions
   */
  deepCopyWithFunctions(source: any): any {
    // Handle null or undefined
    if (source === null || source === undefined) {
      return source;
    }

    // Handle functions - return them as-is
    if (typeof source === 'function') {
      return source;
    }

    // Handle Date objects
    if (source instanceof Date) {
      return new Date(source.getTime());
    }

    // Handle arrays
    if (Array.isArray(source)) {
      return source.map((item) => this.deepCopyWithFunctions(item));
    }

    // Handle objects
    if (typeof source === 'object') {
      const copy: any = {};

      // Copy all properties, recursively if needed
      Object.keys(source).forEach((key) => {
        copy[key] = this.deepCopyWithFunctions(source[key]);
      });

      return copy;
    }

    // For primitive values (string, number, boolean), return as-is
    return source;
  }

  close() {
    if (this.chartInstance) {
      this.chartInstance.destroy();
    }
    this.dialogRef.close();
  }
}
