export interface MenuItem {
  title: string;
  icon?: string;
  link?: string;
  children?: MenuItem[];
  data?: any;
  size?: number;
  category?: string;
  color?: string;
  activeColor?: string;
}

export interface MenuConfig {
  orientation?: 'horizontal' | 'vertical';
  compact?: boolean;
  theme?: 'light' | 'dark';
}
