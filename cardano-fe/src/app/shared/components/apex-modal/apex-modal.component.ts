import { CommonModule } from '@angular/common';
import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { NbIconModule } from '@nebular/theme';
import { MatDialogModule } from '@angular/material/dialog';
import { NgApexchartsModule } from 'ng-apexcharts';
import { ChartOptions } from '../../../pages/dashboard/dashboard.component';
import {
  ApexAxisChartSeries,
  ApexChart,
  ApexFill,
  ApexStroke,
  ApexXAxis,
  ApexDataLabels,
  ApexYAxis,
  ApexGrid,
  ApexMarkers,
  ApexTitleSubtitle,
  ApexTooltip,
  ApexLegend,
} from "ng-apexcharts";

@Component({
  selector: 'app-apex-modal',
  standalone: true,
  imports: [CommonModule, NbIconModule, MatDialogModule, NgApexchartsModule],
  templateUrl: './apex-modal.component.html',
  styleUrl: './apex-modal.component.scss'
})
export class ApexModalComponent {
  public chartApexOptions: ChartOptions = {
    series: [],
    chart: {
      type: "area",
    },
    dataLabels: {
      enabled: false,
    },
    stroke: {
      curve: 'smooth',
      width: 2,
    },
    xaxis: {
      categories: [],
    },
    yaxis: {
      labels: {
        style: {
          colors: '#666',
        },
      },
      title: {
        text: 'ADA',
        style: {
          color: '#666'
        }
      }
    },
    fill: {
      type: 'gradient',
      gradient: {
        shadeIntensity: 1,
        opacityFrom: 0.7,
        opacityTo: 0.3,
        stops: [0, 90, 100],
      },
    },
    title: {
      text: '',
    },
    tooltip: {} as ApexTooltip,
    grid: {} as ApexGrid,
    markers: {} as ApexMarkers,
    colors: ['#A6F6FF', '#3E7EFF', '#238DB4'],
    legend: {
      position: 'top',
      horizontalAlign: 'right',
      floating: true,
      offsetY: -25,
      offsetX: -5
    }
  };
  public title: string;

  constructor(
    public dialogRef: MatDialogRef<ApexModalComponent>,
    @Inject(MAT_DIALOG_DATA)
    public data: { title: string; chartApexOptions: ChartOptions }
  ) {
    this.title = data.title;
    this.chartApexOptions = data.chartApexOptions;
  }

  onClose(): void {
    this.dialogRef.close();
  }
}
