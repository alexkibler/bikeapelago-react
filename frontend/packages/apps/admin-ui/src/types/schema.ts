export interface ColumnMetadata {
  name: string;
  type: string;
  isNullable: boolean;
  isPrimaryKey: boolean;
  isSpatial: boolean;
}

export interface TableMetadata {
  table: string;
  columns: ColumnMetadata[];
}
