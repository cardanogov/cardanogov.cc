import { CommonModule } from '@angular/common';
import { AfterViewInit, Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';
import { NbCardModule, NbButtonModule, NbIconModule } from '@nebular/theme';
import {
  Chart,
  LineController,
  LineElement,
  PointElement,
  LinearScale,
  Title,
  CategoryScale
} from 'chart.js';
Chart.register(LineController, LineElement, PointElement, LinearScale, Title, CategoryScale);
@Component({
  selector: 'app-card',
  standalone: true,
  imports: [NbCardModule, NbButtonModule, NbIconModule, CommonModule],
  templateUrl: './card.component.html',
  styleUrls: ['./card.component.scss']
})
export class CardComponent implements AfterViewInit {
  @Input() title: string = '';
  @Input() icon: string = 'home-outline';
  @Input() iconImg: string = '';
  @Input() value: number = 0;
  @Input() percentageChange: number = 0;
  @Input() isPositive: boolean = true;
  @Input() chartData = [10, 15, 8, 12, 18, 10]; // Fake chart data
  @Input() showChart: boolean = false;
  @Input() showButton: boolean = false;
  @Input() showChange: boolean = false;
  @Output() buttonClick = new EventEmitter<void>();

  @ViewChild('lineCanvas') lineCanvas!: ElementRef<HTMLCanvasElement>;
  chart!: Chart;

  ngAfterViewInit() {
    if (this.showChart && this.lineCanvas) {
      const ctx = this.lineCanvas.nativeElement.getContext('2d');
      if (ctx) {
        this.chart = new Chart(ctx, {
          type: 'line',
          data: {
            labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jan', 'Feb', 'Mar', 'Apr', 'May'],
            datasets: [
              {
                data: [3, 8, 4, 6, 5, 7, 4, 2, -1, 4, 3],
                borderColor: '#00bcd4',
                backgroundColor: 'transparent',
                tension: 0.4,
                pointRadius: 0
              }
            ]
          },
          options: {
            responsive: true,
            plugins: {
              legend: { display: false }
            },
            scales: {
              x: { display: false },
              y: { display: false }
            }
          }
        });
      }
    }
  }

  onClickEvent(event: Event) {
    this.buttonClick.emit();
  }
}