import { ChartConfiguration, ChartData, Plugin } from 'chart.js';
import { generateRandomColorCode } from './color.helper';
import { formatDynamicDecimals, formatValue } from './format.helper';

/**
 * Interface for chart data with metadata
 */
export interface ChartDataWithMeta {
  title: string;
  id?: string;
  total: number;
  percentage: number;
  group?: string;
}

/**
 * Interface for donut chart options
 */
export interface DonutChartOptions {
  useRandomColors?: boolean;
  borderColor?: string;
  borderWidth?: number;
  cutout?: string | number;
  centerText?: {
    title: string;
    subtitle?: string;
    amount?: string;
  };
  rotation?: number;
  circumference?: number;
  spacing?: number;
}

/**
 * Interface for line chart options
 */
export interface LineChartOptions {
  borderColor?: string;
  backgroundColor?: string;
  fill?: boolean;
  tension?: number;
  pointRadius?: number;
  borderWidth?: number;
}

/**
 * Interface for bar chart options
 */
export interface BarChartOptions {
  backgroundColor?: string[];
  borderColor?: string[];
  borderWidth?: number;
  barThickness?: number;
  indexAxis?: 'x' | 'y';
}

/**
 * Creates a line chart configuration
 * @param data Array of data points
 * @param options Optional configuration options
 * @returns ChartConfiguration object
 */
export function createLineChartConfig(
  data: number[],
  options: LineChartOptions = {}
): ChartConfiguration<'line'> {
  const {
    borderColor = '#00b2f8',
    backgroundColor,
    fill = false,
    tension = 0.4,
    pointRadius = 0,
    borderWidth = 2,
  } = options;

  return {
    type: 'line',
    data: {
      labels: data,
      datasets: [
        {
          data,
          borderColor,
          backgroundColor,
          borderWidth,
          tension,
          pointRadius,
          fill,
        },
      ],
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      layout: {
        padding: {
          left: 10,
          right: 10,
          top: 10,
          bottom: 10,
        },
      },
      plugins: {
        legend: {
          display: false,
        },
        tooltip: {
          enabled: false,
        },
      },
      scales: {
        x: {
          display: false,
          grid: {
            display: false,
          },
        },
        y: {
          display: false,
          grid: {
            display: false,
          },
        },
      },
    },
  };
}

/**
 * Creates a bar chart configuration
 * @param labels Array of labels
 * @param data Array of data points
 * @param options Optional configuration options
 * @returns ChartConfiguration object
 */
export function createBarChartConfig(
  labels: string[],
  data: number[],
  options: BarChartOptions = {}
): ChartConfiguration<'bar'> {
  const {
    backgroundColor = ['#2B8CA9'],
    borderColor,
    borderWidth = 0,
    barThickness = 8,
    indexAxis = 'x',
  } = options;

  return {
    type: 'bar',
    data: {
      labels,
      datasets: [
        {
          data,
          backgroundColor,
          borderColor,
          borderWidth,
          barThickness,
        },
      ],
    },
    options: {
      indexAxis,
      responsive: true,
      plugins: {
        legend: {
          display: false,
        },
        tooltip: {
          enabled: true,
          callbacks: {
            label: (context) => {
              return `${context.parsed.x}%`;
            },
          },
        },
      },
      scales: {
        x: {
          beginAtZero: true,
          grid: {
            display: false,
          },
          ticks: {
            stepSize: 1,
            color: '#666',
            font: {
              size: 14,
            },
          },
        },
        y: {
          grid: {
            display: false,
          },
          ticks: {
            color: '#888',
            font: {
              size: 16,
              weight: 'bold',
            },
          },
        },
      },
    },
  };
}

/**
 * Creates a donut chart configuration from data
 * @param data Array of data objects with metadata
 * @param options Optional configuration options
 * @returns ChartConfiguration object
 */
export function createDonutChartConfig(
  data: ChartDataWithMeta[],
  options: DonutChartOptions = {},
  useLabels: boolean = true,
  useRandomColors: boolean = true,
  useCenterText: boolean = false
): ChartConfiguration<'doughnut'> {
  const {
    borderColor = '#ffffff',
    borderWidth = 1,
    cutout = '60%',
    centerText,
    rotation = 0,
    circumference = 360,
    spacing = 1,
  } = options;

  // Extract labels and values from data
  const labels = data.map((item) => item.title);
  const values = data.map((item) => item.total);
  const percentages = data.map((item) => item.percentage);

  // Generate colors for each segment
  const backgroundColor = labels.map((_, index) => {
    // Custom color palette with vibrant colors
    const defaultColors = [
      '#50B432', // Green
      '#FF6384', // Pink
      '#FFCE56', // Yellow
      '#36A2EB', // Blue
      '#4BC0C0', // Teal
      '#9966FF', // Purple
      '#FF9F40', // Orange
      '#C9CBCF', // Gray
      '#FF0000', // Red
      '#00FF00', // Bright Green
      '#0000FF', // Bright Blue
      '#FFFF00', // Yellow
      '#FF00FF', // Magenta
      '#00FFFF', // Cyan
    ];
    return useRandomColors
      ? generateRandomColorCode()
      : defaultColors[index % defaultColors.length];
  });

  const chartData: ChartData<'doughnut'> = {
    labels: useLabels
      ? labels.map((label, i) => `${label} (${percentages[i]}%)`)
      : [],
    datasets: [
      {
        data: values,
        backgroundColor,
        borderColor,
        borderWidth,
        spacing,
        borderRadius: 0,
        hoverOffset: 4,
      },
    ],
  };

  // Create center text plugin
  const centerTextPlugin: Plugin<'doughnut'> = {
    id: 'centerText',
    afterDraw(chart: any) {
      try {
        const { ctx, chartArea, tooltip } = chart;
        const centerX = (chartArea.left + chartArea.right) / 2;
        const centerY = (chartArea.top + chartArea.bottom) / 2;
        const radius =
          Math.min(
            chartArea.right - chartArea.left,
            chartArea.bottom - chartArea.top
          ) / 2;

        ctx.save();
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';

        if (tooltip && tooltip._active && tooltip._active.length > 0) {
          const activePoint = tooltip._active[0];
          const index = activePoint.index;
          const label = data[index].title;
          const subtitle = data[index].percentage;
          const amount = "₳" + formatValue(data[index].total);
          const fontSize = Math.floor(radius * 0.2);
          let words = label.trim().split(/\s+/);
          let newCenterY = centerY - (fontSize / 2) * words.length;
          ctx.font = `bold ${fontSize}px Arial`;
          ctx.fillStyle = '#000';
          for (let i = 0; i < words.length; i++) {
            ctx.fillText(words[i], centerX, newCenterY);
            newCenterY += Math.floor(radius * 0.25);
          }

          const subtitleFontSize = Math.floor(radius * 0.1);
          ctx.font = `${subtitleFontSize}px Arial`;
          ctx.fillStyle = '#666';
          ctx.fillText(formatDynamicDecimals(subtitle) + '%', centerX, newCenterY * 0.95);

          const amountFontSize = Math.floor(radius * 0.085);
          ctx.font = `${amountFontSize}px Arial`;
          ctx.fillStyle = '#000';
          ctx.fillText(amount, centerX, newCenterY * 1.05);
        } else {
          // Draw title
          if (centerText?.title) {
            let words = centerText?.title.trim().split(/\s+/);
            const titleFontSize = Math.floor(radius * 0.2);
            ctx.font = `${titleFontSize}px Arial`;
            ctx.fillStyle = '#000';
            let newCenterY = centerY - (titleFontSize / 2) * words.length;
            for (let i = 0; i < words.length; i++) {
              ctx.fillText(words[i], centerX, newCenterY);
              newCenterY += Math.floor(radius * 0.25);
            }

            // Draw subtitle
            if (centerText?.subtitle) {
              const subtitleFontSize = Math.floor(radius * 0.1);
              ctx.font = `${subtitleFontSize}px Arial`;
              ctx.fillStyle = '#666';
              ctx.fillText(formatDynamicDecimals(centerText?.subtitle) + '%', centerX, newCenterY * 0.95);
            }

            // Draw amount
            if (centerText?.amount) {
              const amountFontSize = Math.floor(radius * 0.085);
              ctx.font = `${amountFontSize}px Arial`;
              ctx.fillStyle = '#000';
              ctx.fillText(centerText?.amount, centerX, newCenterY * 1.05);
            }
          }
        }

        ctx.restore();
      } catch (error) {
        console.error('Error in centerTextPlugin:', error);
      }
    },
  };

  const config: ChartConfiguration<'doughnut'> = {
    type: 'doughnut',
    data: chartData,
    options: {
      layout: {
        padding: 20,
      },
      responsive: true,
      maintainAspectRatio: true,
      cutout,
      rotation,
      circumference,
      plugins: {
        legend: {
          display: useLabels,
          position: 'right',
          align: 'center',
          labels: {
            padding: 10,
            usePointStyle: true,
            boxWidth: 6,
            font: {
              size: 11,
            },
          },
        },
        tooltip: {
          enabled: true,
          callbacks: {
            label: (context) => {
              const value = formatValue(context.raw as number);
              const percentage = formatDynamicDecimals(percentages[context.dataIndex]);
              return `₳${value} (${percentage}%)`;
            },
          },
        },
      },
    },
    plugins: useCenterText ? [centerTextPlugin] : [], // Add the plugin to the chart
  };

  return config;
}
