import React, { useEffect, useState } from 'react';

import axios from 'axios';
import {
  ChevronDown,
  ChevronRight,
  Edit2,
  FileJson,
  Layers,
  MapIcon,
  Plus,
  Search,
  Table as TableIcon,
  Trash2,
  X,
} from 'lucide-react';

import { GenericDataForm } from '../components/ui/GenericDataForm';
import { SpatialPreview } from '../components/ui/SpatialPreview';
import { useAuth } from '../context/AuthContext';
import type { ColumnMetadata } from '../types/schema';

type DataRow = Record<string, unknown>;

interface TableGroup {
  name: string;
  tables: {
    table: string;
    columns: ColumnMetadata[];
  }[];
}

export const DataExplorer: React.FC = () => {
  const { token } = useAuth();
  const [schemaGroups, setSchemaGroups] = useState<TableGroup[]>([]);
  const [selectedTable, setSelectedTable] = useState<string | null>(null);
  const [expandedGroups, setExpandedGroups] = useState<string[]>(['Game Core']);
  const [data, setData] = useState<DataRow[]>([]);
  const [loading, setLoading] = useState(false);
  const [search, setSearch] = useState('');
  const [previewGeo, setPreviewGeo] = useState<unknown>(null);
  const [formMode, setFormMode] = useState<'create' | 'edit' | null>(null);
  const [selectedRow, setSelectedRow] = useState<DataRow | null>(null);

  const fetchSchema = async () => {
    try {
      const res = await axios.get<TableGroup[]>('/api/admin/schema', {
        headers: { Authorization: `Bearer ${token}` },
      });
      setSchemaGroups(res.data);
      const allTables = res.data.flatMap((g) => g.tables);
      if (allTables.length > 0 && !selectedTable)
        setSelectedTable(allTables[0].table);
    } catch (err) {
      console.error('Failed to fetch schema', err);
    }
  };

  const fetchData = async () => {
    if (!selectedTable) return;
    setLoading(true);
    try {
      const res = await axios.get<{ items: DataRow[] }>(
        `/api/admin/data/${selectedTable}`,
        {
          headers: { Authorization: `Bearer ${token}` },
        },
      );
      setData(res.data.items);
    } catch (err) {
      console.error('Failed to fetch data', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void fetchSchema();
  }, [token]);
  useEffect(() => {
    void fetchData();
  }, [selectedTable, token]);

  const toggleGroup = (name: string) => {
    setExpandedGroups((prev) =>
      prev.includes(name) ? prev.filter((g) => g !== name) : [...prev, name],
    );
  };

  const handleSave = async (payload: DataRow) => {
    try {
      const pk = currentTable?.columns.find((c) => c.isPrimaryKey)?.name;
      const pkValue = pk ? payload[pk] : undefined;
      if (formMode === 'edit' && pk && pkValue == null) {
        throw new Error(`Missing primary key value for ${selectedTable}`);
      }
      const url =
        formMode === 'edit' && pk
          ? `/api/admin/data/${selectedTable}/${pkValue as string | number | boolean}`
          : `/api/admin/data/${selectedTable}`;
      const method = formMode === 'edit' ? 'put' : 'post';

      await axios({
        method,
        url,
        data: payload,
        headers: { Authorization: `Bearer ${token}` },
      });
      setFormMode(null);
      void fetchData();
    } catch (err) {
      console.error('Save failed', err);
    }
  };

  const handleDelete = async (row: DataRow) => {
    if (!window.confirm('Delete this record irreversibly?')) return;
    try {
      const pk = currentTable?.columns.find((c) => c.isPrimaryKey)?.name;
      if (pk) {
        await axios.delete(`/api/admin/data/${selectedTable}/${String(row[pk])}`, {
          headers: { Authorization: `Bearer ${token}` },
        });
        void fetchData();
      }
    } catch (err) {
      console.error('Delete failed', err);
    }
  };

  const allTables = schemaGroups.flatMap((g) => g.tables);
  const currentTable = allTables.find((s) => s.table === selectedTable);
  const filteredData = data.filter((row) =>
    Object.values(row).some((v) =>
      String(v).toLowerCase().includes(search.toLowerCase()),
    ),
  );

  const renderCellValue = (value: unknown) => {
    if (value === null || value === undefined) return '-';
    if (
      typeof value === 'string' ||
      typeof value === 'number' ||
      typeof value === 'boolean'
    ) {
      return String(value);
    }

    try {
      return JSON.stringify(value);
    } catch {
      return '-';
    }
  };

  return (
    <div className='flex flex-col h-full bg-[#09090b] border border-[#18181b] rounded-[40px] overflow-hidden shadow-2xl relative'>
      {/* Search Header */}
      <div className='p-8 border-b border-[#18181b] flex flex-wrap items-center justify-between gap-6 bg-zinc-950/20'>
        <div className='flex items-center gap-4'>
          <div className='w-12 h-12 bg-zinc-900 border border-zinc-800 rounded-2xl flex items-center justify-center text-zinc-400 group hover:border-indigo-500/50 transition-colors'>
            <Layers size={24} />
          </div>
          <div>
            <h2 className='text-2xl font-black text-white tracking-tight font-outfit uppercase'>
              Database Explorer
            </h2>
            <p className='text-xs text-zinc-600 font-bold tracking-widest uppercase'>
              Browse and manage raw data collections
            </p>
          </div>
        </div>

        <div className='flex-1 max-w-xl flex items-center gap-4'>
          <div className='flex-1 relative group'>
            <Search
              className='absolute left-6 top-1/2 -translate-y-1/2 text-zinc-600 group-focus-within:text-indigo-400 transition-colors'
              size={20}
            />
            <input
              type='text'
              placeholder='Search across collections...'
              className='w-full bg-[#030304] border border-[#18181b] rounded-3xl pl-16 pr-6 py-4 text-zinc-300 focus:outline-none focus:ring-1 focus:ring-indigo-500/50 transition-all font-medium text-sm placeholder:text-zinc-800'
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
          </div>
          <button
            onClick={() => {
              setSelectedRow(null);
              setFormMode('create');
            }}
            className='h-14 bg-indigo-600 hover:bg-indigo-500 text-white px-8 rounded-2xl text-sm font-black flex items-center transition-all shadow-xl shadow-indigo-600/10 active:scale-95 uppercase tracking-widest'
          >
            <Plus size={20} className='mr-3' />
            Add Entry
          </button>
        </div>
      </div>

      <div className='flex flex-1 min-h-0'>
        {/* Table Sidebar Picker */}
        <aside className='w-80 border-r border-[#18181b] overflow-y-auto custom-scrollbar flex flex-col bg-[#09090b]/40'>
          <div className='p-6 space-y-4'>
            {schemaGroups.map((group) => (
              <div key={group.name} className='space-y-1'>
                <button
                  onClick={() => toggleGroup(group.name)}
                  className='w-full flex items-center gap-2 px-3 py-2 text-[10px] font-black text-zinc-600 uppercase tracking-[0.2em] hover:text-white transition-colors group'
                >
                  {expandedGroups.includes(group.name) ? (
                    <ChevronDown
                      size={14}
                      className='text-zinc-800 group-hover:text-zinc-400'
                    />
                  ) : (
                    <ChevronRight
                      size={14}
                      className='text-zinc-800 group-hover:text-zinc-400'
                    />
                  )}
                  {group.name}
                  <div className='ml-auto w-1 h-1 rounded-full bg-zinc-800' />
                </button>

                {expandedGroups.includes(group.name) && (
                  <div className='space-y-1 ml-1 pl-4 border-l border-zinc-900 py-1'>
                    {group.tables.map((table) => (
                      <button
                        key={table.table}
                        onClick={() => setSelectedTable(table.table)}
                        className={`
                           w-full flex items-center gap-3 px-3 py-3 rounded-xl transition-all group relative
                           ${
                             selectedTable === table.table
                               ? 'bg-zinc-100 text-zinc-950 font-black shadow-md'
                               : 'text-zinc-500 hover:bg-zinc-900 hover:text-zinc-200'
                           }
                         `}
                      >
                        <TableIcon
                          size={14}
                          className={
                            selectedTable === table.table
                              ? 'text-zinc-950'
                              : 'text-zinc-700 group-hover:text-zinc-400'
                          }
                        />
                        <span className='flex-1 text-left text-xs tracking-tight truncate'>
                          {table.table}
                        </span>
                        {selectedTable === table.table && (
                          <div className='absolute right-3 w-1.5 h-1.5 rounded-full bg-indigo-500' />
                        )}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            ))}
          </div>
        </aside>

        {/* Dynamic Table Area */}
        <div className='flex-1 overflow-x-auto overflow-y-auto custom-scrollbar bg-[#020203]'>
          {loading ? (
            <div className='flex items-center justify-center h-full gap-4'>
              <div className='w-8 h-8 border-3 border-zinc-800 border-t-indigo-500 rounded-full animate-spin' />
              <span className='text-zinc-500 font-black tracking-widest uppercase text-sm'>
                Loading Data
              </span>
            </div>
          ) : (
            <table className='w-full text-left border-collapse min-w-[800px]'>
              <thead className='sticky top-0 bg-[#09090b] border-b border-[#18181b] z-20'>
                <tr>
                  {currentTable?.columns.map((col) => (
                    <th
                      key={col.name}
                      className='p-6 text-[10px] font-black text-zinc-600 uppercase tracking-[0.2em] whitespace-nowrap'
                    >
                      <div className='flex items-center gap-2'>
                        {col.name}
                        {col.isPrimaryKey && (
                          <div className='p-1 px-1.5 bg-indigo-500/10 text-indigo-500 border border-indigo-500/20 rounded text-[8px]'>
                            PK
                          </div>
                        )}
                      </div>
                    </th>
                  ))}
                  <th className='p-6 sticky right-0 bg-[#09090b] text-[10px] font-black text-zinc-600 uppercase tracking-[0.2em] text-center border-l border-[#18181b]'>
                    Actions
                  </th>
                </tr>
              </thead>
              <tbody className='divide-y divide-[#18181b]'>
                {filteredData.map((row, idx) => (
                  <tr
                    key={idx}
                    className='group hover:bg-zinc-900/30 transition-all'
                  >
                    {currentTable?.columns.map((col) => {
                      const val = row[col.name];
                      return (
                        <td
                          key={col.name}
                          className='p-6 text-[13px] text-zinc-400 font-medium tracking-tight max-w-xs truncate border-r border-[#18181b]/10'
                        >
                          {col.isSpatial ? (
                            <button
                              onClick={() => setPreviewGeo(val)}
                              className='flex items-center text-emerald-400 bg-emerald-500/5 hover:bg-emerald-500/10 px-4 py-2 rounded-2xl border border-emerald-500/10 transition-all hover:scale-105 active:scale-95 font-bold text-[11px] uppercase tracking-widest'
                            >
                              <MapIcon size={16} className='mr-3' />
                              View Map
                            </button>
                          ) : typeof val === 'object' && val !== null ? (
                            <div className='px-3 py-1.5 bg-zinc-950 border border-zinc-800 rounded-xl text-[10px] text-zinc-600 font-mono'>
                              {JSON.stringify(val).slice(0, 30)}...
                            </div>
                          ) : (
                            <span
                              className={
                                col.isPrimaryKey
                                  ? 'text-white font-black font-mono'
                                  : ''
                              }
                            >
                              {renderCellValue(val)}
                            </span>
                          )}
                        </td>
                      );
                    })}
                    <td className='p-6 sticky right-0 bg-[#020203] group-hover:bg-[#09090b] transition-colors shadow-[-20px_0_30px_-10px_rgba(0,0,0,0.8)] border-l border-[#18181b]'>
                      <div className='flex items-center gap-3 justify-center'>
                        <button
                          onClick={() => {
                            setSelectedRow(row);
                            setFormMode('edit');
                          }}
                          className='p-3 bg-zinc-900 text-zinc-600 hover:text-indigo-400 hover:border-indigo-500/30 border border-zinc-800 rounded-2xl transform hover:scale-110 active:scale-95 transition-all shadow-xl'
                        >
                          <Edit2 size={18} />
                        </button>
                        <button
                          onClick={() => handleDelete(row)}
                          className='p-3 bg-zinc-900 text-zinc-600 hover:text-red-400 hover:border-red-500/30 border border-zinc-800 rounded-2xl transform hover:scale-110 active:scale-95 transition-all shadow-xl'
                        >
                          <Trash2 size={18} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>

      {/* Spatial Preview Modal */}
      {previewGeo !== null && previewGeo !== undefined && (
        <div className='fixed inset-0 z-[100] flex items-center justify-center p-8 animate-in fade-in duration-300'>
          <div
            className='absolute inset-0 bg-zinc-950/80 backdrop-blur-2xl'
            onClick={() => setPreviewGeo(null)}
          />
          <div className='bg-zinc-900 border border-zinc-800 w-full max-w-5xl rounded-[48px] overflow-hidden shadow-[0_0_100px_rgba(0,0,0,1)] relative z-10 animate-in zoom-in-95 slide-in-from-bottom-8 duration-500 border-indigo-500/10'>
            <div className='p-10 border-b border-zinc-800 flex items-center justify-between bg-zinc-950/40'>
              <div className='flex items-center gap-6'>
                <div className='w-16 h-16 bg-emerald-500/10 border border-emerald-500/20 rounded-[32px] flex items-center justify-center text-emerald-400'>
                  <MapIcon size={32} />
                </div>
                <div>
                  <h3 className='font-black text-3xl text-white tracking-tighter font-outfit uppercase'>
                    Spatial Preview
                  </h3>
                  <p className='text-zinc-500 font-bold tracking-[0.2em] text-[10px] uppercase'>
                    Geometric data visualization
                  </p>
                </div>
              </div>
              <button
                onClick={() => setPreviewGeo(null)}
                className='p-4 bg-zinc-800 hover:bg-zinc-700 rounded-3xl text-zinc-400 hover:text-white transition-all shadow-inner'
              >
                <X size={28} />
              </button>
            </div>
            <div className='p-10 bg-[#020203]'>
              <SpatialPreview
                geoJson={previewGeo}
                height='500px'
                interactive={true}
              />
              <div className='mt-10 p-6 bg-zinc-950/80 rounded-[32px] border border-zinc-800/50 flex items-center gap-6'>
                <div className='p-3 bg-zinc-900 rounded-2xl text-zinc-600'>
                  <FileJson size={20} />
                </div>
                <pre className='flex-1 text-[11px] text-emerald-400/80 font-mono break-all line-clamp-1 opacity-50 italic'>
                  {JSON.stringify(previewGeo).slice(0, 500)}...
                </pre>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Dynamic Data Form Modal */}
      {formMode && currentTable && (
          <GenericDataForm
            table={selectedTable!}
            columns={currentTable.columns}
            initialData={selectedRow ?? undefined}
            onSave={handleSave}
            onCancel={() => setFormMode(null)}
          />
      )}
    </div>
  );
};
