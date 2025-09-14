import { CommonModule } from '@angular/common';
import {
  Component,
  Input,
  OnInit,
  OnDestroy,
  ElementRef,
  ViewChild,
  Output,
  EventEmitter,
  OnChanges,
  AfterViewInit,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import * as d3 from 'd3';

export interface VennSet {
  id: number;
  label: string;
  color: string;
  count: number;
  description: string;
}

export interface VennDiagramConfig {
  width?: number;
  height?: number;
  animationDuration?: number;
  enableTooltip?: boolean;
}

@Component({
  selector: 'app-venn-diagram',
  templateUrl: './venn-diagram.component.html',
  styleUrls: ['./venn-diagram.component.scss'],
  imports: [CommonModule],
  standalone: true,
})
export class VennDiagramComponent
  implements OnInit, OnDestroy, OnChanges, AfterViewInit
{
  @ViewChild('svgContainer', { static: true }) svgContainer!: ElementRef;

  @Input() sets: VennSet[] = [
    {
      id: 1,
      label: 'DReps Voting Power',
      color: '#00d4ff', // Sáng hơn - màu xanh dương sáng
      count: 50,
      description: 'Active voting power in governance',
    },
    {
      id: 2,
      label: 'ADA Delegation',
      color: '#ffd700', // Sáng hơn - màu vàng sáng
      count: 120,
      description: 'Delegated ADA tokens',
    },
    {
      id: 3,
      label: 'ADA Staking',
      color: '#32cd32', // Sáng hơn - màu xanh lá sáng
      count: 250,
      description: 'Staked ADA tokens',
    },
    {
      id: 4,
      label: 'Circulating Supply',
      color: '#ff6347', // Sáng hơn - màu cam đỏ sáng
      count: 400,
      description: 'Total circulating ADA',
    },
    {
      id: 5,
      label: 'Max Supply',
      color: '#9370db', // Sáng hơn - màu tím sáng
      count: 600,
      description: 'Maximum ADA supply',
    },
  ];
  @Input() showTitle: boolean = true;

  @Output() setClicked = new EventEmitter<VennSet>();
  @Output() setsUpdated = new EventEmitter<VennSet[]>();
  @Output() expand = new EventEmitter<void>();

  private svg: any;
  private tooltip: any;
  private svgWidth: number = 0;
  private svgHeight: number = 0;
  private centerX: number = 0;
  private centerY: number = 0;
  private right: number = 140;
  private resizeListener?: () => void;

  constructor(private dialog: MatDialog) {}

  ngOnInit(): void {

  }

  ngAfterViewInit(): void {
    if (this.svgContainer?.nativeElement) {
      setTimeout(() => {
        this.initializeDiagram();
        this.createDiagram();
      }, 100);
    }

    // Add resize listener for mobile responsiveness
    this.resizeListener = () => {
      if (this.svgContainer?.nativeElement) {
        setTimeout(() => {
          this.initializeDiagram();
          this.createDiagram();
        }, 100);
      }
    };
    window.addEventListener('resize', this.resizeListener);
  }

  ngOnDestroy(): void {
    if (this.svg) {
      this.svg.selectAll('*').remove();
    }
    if (this.tooltip) {
      this.tooltip.remove();
    }
    if (this.resizeListener) {
      window.removeEventListener('resize', this.resizeListener);
    }
  }

  ngOnChanges() {
    if (this.svgContainer?.nativeElement) {
      setTimeout(() => {
        this.initializeDiagram();
        this.createDiagram();
      }, 100);
    }
  }

  private initializeDiagram(): void {
    // Get actual SVG dimensions
    const svgElement = this.svgContainer.nativeElement;
    const rect = svgElement.getBoundingClientRect();
    if (rect.width == 0 || rect.height == 0) return;

    this.svgWidth = rect.width || 0;
    this.svgHeight = rect.height - 120 || 0;

    // Check if mobile device
    const isMobile = window.innerWidth <= 768;
    const isSmallMobile = window.innerWidth <= 480;

    // Adjust dimensions and positioning for mobile
    if (isMobile) {
      this.right = isSmallMobile ? 80 : 100;
      this.svgHeight = Math.max(this.svgHeight, isSmallMobile ? 350 : 400);
    } else {
      this.right = 140;
    }

    // Set center points based on actual SVG size and device type
    this.centerX = this.svgWidth / 2 + this.right / 3;
    this.centerY = this.svgHeight / 2 - 10;

    // Initialize SVG with proper dimensions and optimization
    this.svg = d3
      .select(svgElement)
      .attr('width', this.svgWidth)
      .attr('height', this.svgHeight)
      .attr('viewBox', `0 0 ${this.svgWidth} ${this.svgHeight}`)
      .attr('preserveAspectRatio', 'xMidYMid meet')
      .style('shape-rendering', 'geometricPrecision')
      .style('text-rendering', 'optimizeLegibility');

    // Initialize tooltip
    d3.select('body').selectAll('.venn-tooltip').remove();

    // Create new tooltip with mobile-optimized styling
    this.tooltip = d3
      .select('body')
      .append('div')
      .attr('class', 'venn-tooltip')
      .style('position', 'absolute')
      .style('background', 'rgba(0, 0, 0, 0.8)')
      .style('color', 'white')
      .style('padding', isMobile ? '8px' : '10px')
      .style('border-radius', '8px')
      .style('font-size', isMobile ? '12px' : '14px')
      .style('pointer-events', 'none')
      .style('opacity', 0)
      .style('z-index', '1000')
      .style('transition', 'opacity 0.3s')
      .style('max-width', isMobile ? '200px' : '250px')
      .style('word-wrap', 'break-word');
  }

  private calculatePositions(): Array<{ x: number; y: number; r: number }> {
    const positions: Array<{ x: number; y: number; r: number }> = [];

    // Check if mobile device
    const isMobile = window.innerWidth <= 768;
    const isSmallMobile = window.innerWidth <= 480;

    // Adjust radius calculation based on screen size
    let radiusBaseMul: number;
    let maxRadiusLimit: number;

    if (isSmallMobile) {
      radiusBaseMul = 0.35;
      maxRadiusLimit = 120;
    } else if (isMobile) {
      radiusBaseMul = 0.4;
      maxRadiusLimit = 150;
    } else {
      radiusBaseMul = this.svgWidth < 700 ? 0.45 : 0.55;
      maxRadiusLimit = 300;
    }

    const maxRadius = Math.min((Math.min(this.svgHeight, this.svgWidth)) * radiusBaseMul, maxRadiusLimit);
    const minRadius = maxRadius * 0.08;

    // Tạo tỷ lệ ánh xạ count (0-100) sang bán kính (minRadius - maxRadius)
    const radiusScale = d3
      .scaleLinear()
      .domain([0, 100])
      .range([minRadius, maxRadius]);

    // Tìm vòng lớn nhất (count = 100) để xác định cạnh dưới
    let maxRadiusActual = 0;
    let maxRadiusIndex = 0;
    for (let i = 0; i < this.sets.length; i++) {
      const radius = radiusScale(this.sets[i].count);
      if (radius > maxRadiusActual) {
        maxRadiusActual = radius;
        maxRadiusIndex = i;
      }
    }

    // Tọa độ Y của cạnh dưới của vòng lớn nhất
    const bottomEdgeY = this.centerY + maxRadiusActual;

    // Tính vị trí cho từng tập hợp
    for (let i = 0; i < this.sets.length; i++) {
      const set = this.sets[i];
      const radius = radiusScale(set.count);

      // Tính tọa độ Y sao cho cạnh dưới của vòng tròn thẳng hàng với vòng lớn nhất
      const y = bottomEdgeY - radius;

      positions.push({
        x: this.centerX,
        y: y,
        r: radius,
      });
    }

    return positions;
  }

  public createDiagram(): void {
    if (!this.svg || this.svgWidth === 0 || this.svgHeight === 0) return;

    // Clear previous diagram
    this.svg.selectAll('*').remove();

    const positions = this.calculatePositions();

    // Create background
    this.svg
      .append('rect')
      .attr('width', this.svgWidth)
      .attr('height', this.svgHeight)
      .attr('fill', 'rgba(255, 255, 255, 0.1)')
      .attr('rx', 15);

    // Create circles from largest to smallest (reverse order for proper layering)
    for (let i = this.sets.length - 1; i >= 0; i--) {
      const set = this.sets[i];
      const pos = positions[i];

      const circle = this.svg
        .append('circle')
        .attr('class', `circle circle-${set.id}`)
        .attr('cx', pos.x - this.right)
        .attr('cy', pos.y)
        .attr('r', 0) // Start with radius 0 for animation
        .attr('fill', set.color)
        .attr('stroke', d3.color(set.color)?.darker(0.5))
        .style('fill-opacity', 0.6) // Tăng độ trong suốt từ 0.3 lên 0.6
        .style('stroke-width', 2)
        .style('cursor', 'pointer')
        .style('transition', 'all 0.3s ease') // Thêm transition mượt mà
        .on('mouseover', (event: MouseEvent) =>
          this.handleMouseOver(event, set)
        )
        .on('mouseout', (event: MouseEvent) => this.handleMouseOut(event, set))
        .on('click', () => this.handleClick(set));

      // Animate circle appearance
      circle
        .transition()
        .duration(500)
        .delay(i * 100)
        .attr('r', pos.r)
        .style('opacity', 1);
    }

    // Check if mobile device for legend optimization
    const isMobile = window.innerWidth <= 768;
    const isSmallMobile = window.innerWidth <= 480;

    // Add legend on the right
    const legend = this.svg
      .append('g')
      .attr('class', 'legend')
      .attr(
        'transform',
        `translate(${this.svgWidth - this.right}, ${
          this.svgHeight / 2 - this.sets.length * (isMobile ? 8 : 12)
        })`
      );

    this.sets.forEach((set, i) => {
      const legendItem = legend
        .append('g')
        .attr('transform', `translate(0, ${i * (isMobile ? 20 : 25)})`);

      // Add color square with mobile-optimized size
      const squareSize = isSmallMobile ? 10 : (isMobile ? 12 : 15);
      legendItem
        .append('rect')
        .attr('x', 0)
        .attr('y', 0)
        .attr('width', squareSize)
        .attr('height', squareSize)
        .attr('fill', set.color)
        .style('fill-opacity', 0.6) // Đồng bộ với độ trong suốt của circles
        .style('stroke', d3.color(set.color)?.darker(0.5))
        .style('stroke-width', isMobile ? 1 : 2);

      // Add label with mobile-optimized font size
      const fontSize = isSmallMobile ? '9px' : (isMobile ? '10px' : '12px');
      const labelX = isSmallMobile ? 15 : (isMobile ? 18 : 25);
      const labelY = isSmallMobile ? 8 : (isMobile ? 9 : 12);

      legendItem
        .append('text')
        .attr('x', labelX)
        .attr('y', labelY)
        .style('font-size', fontSize)
        .style('fill', '#333')
        .text(set.label);
    });

    // Add labels on top of each circle
    const positionsWithOffset = positions.map((pos) => ({
      x: pos.x - this.right,
      y: pos.y,
      r: pos.r,
    }));

    // Mobile-optimized font sizes
    const minFontSize = isSmallMobile ? 6 : (isMobile ? 8 : 10); // Font size nhỏ nhất
    const maxFontSize = isSmallMobile ? 12 : (isMobile ? 14 : 20); // Font size lớn nhất
    const radiusScale = d3
      .scaleLinear()
      .domain([
        d3.min(positions, (d) => d.r) || 0,
        d3.max(positions, (d) => d.r) || 100,
      ])
      .range([minFontSize, maxFontSize])
      .clamp(true);

    this.sets.forEach((set, i) => {
      const pos = positionsWithOffset[i];
      const fontSize = radiusScale(pos.r); // Tùy chỉnh font-size dựa trên bán kính
      const labelOffset = isMobile ? 3 : 5; // Giảm khoảng cách cho mobile

      this.svg
        .append('text')
        .attr('class', `circle-label label-${set.id}`)
        .attr('x', pos.x)
        .attr('y', pos.y - pos.r - labelOffset) // Đặt nhãn ngay trên cạnh trên của vòng tròn
        .style('font-size', `${fontSize}px`)
        .style('font-weight', 'bold')
        .style('text-anchor', 'middle')
        .style('fill', '#333')
        .style('pointer-events', 'none')
        .style('opacity', 0)
        .text(set.label)
        .transition()
        .duration(300)
        .delay(i * 100 + 300)
        .style('opacity', 1);
    });
  }

  private handleMouseOver(event: MouseEvent, set: VennSet): void {
    if (!this.tooltip) return;

    // Highlight the hovered circle với hiệu ứng mượt mà hơn
    d3.select(`.circle-${set.id}`)
      .style('fill-opacity', 0.9) // Tăng độ trong suốt khi hover
      .style('stroke-width', 3)
      .style('filter', 'drop-shadow(0 4px 8px rgba(0,0,0,0.3))'); // Thêm shadow effect

    // Show tooltip
    this.tooltip
      .style('opacity', 1)
      .html(
        `
        <strong>${set.label}</strong><br/>
        <em>${set.count} %</em>
      `
      )
      .style('left', event.pageX + 10 + 'px')
      .style('top', event.pageY - 10 + 'px');
  }

  private handleMouseOut(event: MouseEvent, set: VennSet): void {
    if (!this.tooltip) return;

    // Reset circle appearance về trạng thái mặc định sáng hơn
    d3.select(`.circle-${set.id}`)
      .style('fill-opacity', 0.6) // Về lại độ trong suốt mặc định sáng hơn
      .style('stroke-width', 2)
      .style('filter', 'none'); // Loại bỏ shadow effect

    // Hide tooltip
    this.tooltip.style('opacity', 0);
  }

  private handleClick(set: VennSet): void {
    this.setClicked.emit(set);

    // Add click animation với hiệu ứng pulse
    d3.select(`.circle-${set.id}`)
      .transition()
      .duration(150)
      .style('fill-opacity', 1)
      .style('transform', 'scale(1.05)')
      .transition()
      .duration(150)
      .style('fill-opacity', 0.6)
      .style('transform', 'scale(1)');
  }

  public updateSet(index: number, updates: Partial<VennSet>): void {
    if (index >= 0 && index < this.sets.length) {
      this.sets[index] = { ...this.sets[index], ...updates };
      this.createDiagram();
      this.setsUpdated.emit(this.sets);
    }
  }

  public animateNesting(): void {
    const positions = this.calculatePositions();

    this.svg
      .selectAll('.circle')
      .transition()
      .duration(1000)
      .attr(
        'r',
        (d: any, i: number) => positions[this.sets.length - 1 - i].r * 1.1
      )
      .transition()
      .duration(1000)
      .attr('r', (d: any, i: number) => positions[this.sets.length - 1 - i].r);
  }

  onExpand() {
    this.expand.emit();
  }
}
