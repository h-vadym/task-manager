import { useEffect, useMemo, useState, type FormEvent, type ReactNode } from "react";
import { NavLink, Navigate, Route, Routes } from "react-router-dom";
import { api } from "./api";
import {
  taskPriorities,
  taskStatuses,
  taskTypes,
  type TaskDraft,
  type TaskItem,
  type TaskStatus,
  type TaskTypeStat,
} from "./types";

const emptyDraft: TaskDraft = {
  title: "", description: "", type: "develop", status: "to do", priority: "normal", assigneeId: "",
};

const statusLabels: Record<TaskStatus, string> = { "to do": "Заплановано", "in progress": "У роботі", done: "Готово" };
const typeLabels = { design: "Дизайн", develop: "Розробка", review: "Ревʼю", testing: "Тестування" };

export function App() {
  return <main className="min-h-screen bg-slate-950 text-slate-100"><Shell><Routes>
    <Route path="/tasks" element={<TasksPage />} />
    <Route path="/analytics/task-types" element={<AnalyticsPage />} />
    <Route path="*" element={<Navigate to="/tasks" replace />} />
  </Routes></Shell></main>;
}

function Shell({ children }: { children: ReactNode }) {
  return <div className="mx-auto flex min-h-screen max-w-[1520px] flex-col px-4 py-4 sm:px-6 lg:px-8">
    <header className="mb-7 flex items-center justify-between rounded-2xl border border-white/10 bg-white/[0.04] px-5 py-4 backdrop-blur sm:mb-10">
      <NavLink to="/tasks" className="flex items-center gap-3"><span className="grid h-9 w-9 place-items-center rounded-xl bg-lime-300 font-black text-slate-950">O</span><span><b className="block text-sm tracking-wide">ORBIT</b><span className="text-xs text-slate-400">Task workspace</span></span></NavLink>
      <nav className="flex rounded-xl bg-slate-900 p-1 text-sm">
        <NavItem to="/tasks">Задачі</NavItem><NavItem to="/analytics/task-types">Аналітика</NavItem>
      </nav>
    </header>{children}
  </div>;
}

function NavItem({ to, children }: { to: string; children: ReactNode }) {
  return <NavLink to={to} className={({ isActive }) => `rounded-lg px-3 py-2 transition ${isActive ? "bg-white text-slate-950 shadow-sm" : "text-slate-400 hover:text-white"}`}>{children}</NavLink>;
}

function TasksPage() {
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [query, setQuery] = useState("");
  const [type, setType] = useState<"all" | TaskItem["type"]>("all");
  const [editing, setEditing] = useState<TaskItem | null | "new">(null);

  const load = async () => { setLoading(true); setError(""); try { setTasks(await api.getTasks()); } catch (e) { setError(messageOf(e)); } finally { setLoading(false); } };
  useEffect(() => { void load(); }, []);
  const filtered = useMemo(() => tasks.filter((task) => (type === "all" || task.type === type) && `${task.title} ${task.description}`.toLowerCase().includes(query.toLowerCase())), [tasks, query, type]);
  const updateStatus = async (task: TaskItem, status: TaskStatus) => { try { const changed = await api.updateStatus(task.id, status); setTasks((items) => items.map((item) => item.id === task.id ? changed : item)); } catch (e) { setError(messageOf(e)); } };
  const remove = async (task: TaskItem) => { if (!confirm(`Видалити «${task.title}»?`)) return; try { await api.deleteTask(task.id); setTasks((items) => items.filter((item) => item.id !== task.id)); } catch (e) { setError(messageOf(e)); } };
  const save = async (draft: TaskDraft, current: TaskItem | null) => { try { const saved = current ? await api.updateTask(current.id, draft) : await api.createTask(draft); setTasks((items) => current ? items.map((item) => item.id === saved.id ? saved : item) : [saved, ...items]); setEditing(null); } catch (e) { throw e; } };

  return <section>
    <div className="mb-8 flex flex-col justify-between gap-5 md:flex-row md:items-end"><div><p className="mb-2 text-sm font-medium text-lime-300">ВАША РОБОЧА ДОШКА</p><h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">Задачі, що рухають справу</h1><p className="mt-2 max-w-xl text-slate-400">Плануйте, тримайте фокус і доводьте роботу до результату.</p></div><button onClick={() => setEditing("new")} className="btn-primary">+ Нова задача</button></div>
    <div className="mb-6 flex flex-col gap-3 rounded-2xl border border-white/10 bg-slate-900/60 p-3 sm:flex-row"><label className="relative flex-1"><span className="sr-only">Пошук задач</span><input value={query} onChange={(e) => setQuery(e.target.value)} placeholder="Пошук за назвою або описом..." className="field pl-10"/><span className="absolute left-3 top-2.5 text-slate-500">⌕</span></label><select value={type} onChange={(e) => setType(e.target.value as typeof type)} className="field sm:w-44"><option value="all">Усі напрями</option>{taskTypes.map((item) => <option key={item} value={item}>{typeLabels[item]}</option>)}</select></div>
    {error && <Notice tone="error" text={error} action={<button onClick={() => void load()} className="underline">Спробувати знову</button>} />}
    {loading ? <LoadingBoard /> : <div className="grid gap-5 xl:grid-cols-3">{taskStatuses.map((status) => <TaskColumn key={status} status={status} tasks={filtered.filter((task) => task.status === status)} onEdit={setEditing} onStatus={updateStatus} onDelete={remove} />)}</div>}
    {!loading && !error && filtered.length === 0 && <div className="mt-6 rounded-2xl border border-dashed border-slate-700 py-16 text-center text-slate-400">За цими фільтрами задач не знайдено.</div>}
    {editing && <TaskDialog task={editing === "new" ? null : editing} onClose={() => setEditing(null)} onSave={save} />}
  </section>;
}

function TaskColumn({ status, tasks, onEdit, onStatus, onDelete }: { status: TaskStatus; tasks: TaskItem[]; onEdit: (task: TaskItem) => void; onStatus: (task: TaskItem, status: TaskStatus) => void; onDelete: (task: TaskItem) => void }) {
  return <section className="rounded-2xl bg-white/[0.035] p-4 ring-1 ring-white/10"><div className="mb-4 flex items-center justify-between"><h2 className="font-semibold">{statusLabels[status]}</h2><span className="rounded-full bg-white/10 px-2.5 py-1 text-xs text-slate-300">{tasks.length}</span></div><div className="space-y-3">{tasks.map((task) => <article key={task.id} className="group rounded-xl border border-white/10 bg-slate-900 p-4 shadow-sm transition hover:-translate-y-0.5 hover:border-lime-300/40"><div className="mb-3 flex items-start justify-between gap-3"><span className={`badge badge-${task.type}`}>{typeLabels[task.type]}</span><button onClick={() => onEdit(task)} className="text-slate-500 hover:text-white" aria-label={`Редагувати ${task.title}`}>•••</button></div><h3 className="font-medium leading-snug">{task.title}</h3>{task.description && <p className="mt-2 line-clamp-2 text-sm leading-relaxed text-slate-400">{task.description}</p>}<div className="mt-4 flex items-center justify-between border-t border-white/10 pt-3"><span className={`priority priority-${task.priority}`}>{task.priority === "high" ? "Високий" : task.priority === "normal" ? "Звичайний" : "Низький"}</span><select aria-label="Змінити статус" value={task.status} onChange={(e) => onStatus(task, e.target.value as TaskStatus)} className="max-w-28 bg-transparent text-xs text-slate-400 outline-none hover:text-white"><option value="to do">Заплановано</option><option value="in progress">У роботі</option><option value="done">Готово</option></select></div><button onClick={() => onDelete(task)} className="mt-3 text-xs text-slate-600 opacity-0 transition hover:text-rose-300 group-hover:opacity-100">Видалити</button></article>)}</div></section>;
}

function TaskDialog({ task, onClose, onSave }: { task: TaskItem | null; onClose: () => void; onSave: (draft: TaskDraft, task: TaskItem | null) => Promise<void> }) {
  const [draft, setDraft] = useState<TaskDraft>(task ? { title: task.title, description: task.description, type: task.type, status: task.status, priority: task.priority, assigneeId: task.assigneeId ?? "" } : emptyDraft);
  const [error, setError] = useState(""); const [saving, setSaving] = useState(false);
  const change = <K extends keyof TaskDraft>(key: K, value: TaskDraft[K]) => setDraft((previous) => ({ ...previous, [key]: value }));
  const submit = async (event: FormEvent) => { event.preventDefault(); if (!draft.title.trim() || !draft.description.trim()) { setError("Додайте назву та короткий опис задачі."); return; } setSaving(true); setError(""); try { await onSave({ ...draft, title: draft.title.trim(), description: draft.description.trim() }, task); } catch (e) { setError(messageOf(e)); setSaving(false); } };
  return <div role="dialog" aria-modal="true" className="fixed inset-0 z-20 grid place-items-center bg-slate-950/80 p-4 backdrop-blur-sm"><form onSubmit={submit} className="w-full max-w-xl rounded-2xl border border-white/10 bg-slate-900 p-6 shadow-2xl"><div className="mb-6 flex items-start justify-between"><div><p className="text-xs font-medium text-lime-300">{task ? "РЕДАГУВАННЯ" : "НОВА ЗАДАЧА"}</p><h2 className="mt-1 text-xl font-semibold">{task ? "Оновіть деталі" : "Додайте до дошки"}</h2></div><button type="button" onClick={onClose} className="text-xl text-slate-400 hover:text-white" aria-label="Закрити">×</button></div>{error && <Notice tone="error" text={error}/>}<div className="grid gap-4"><label className="label">Назва<input autoFocus value={draft.title} onChange={(e) => change("title", e.target.value)} className="field mt-1.5" maxLength={140}/></label><label className="label">Опис<textarea value={draft.description} onChange={(e) => change("description", e.target.value)} className="field mt-1.5 min-h-24 resize-y" maxLength={2000}/></label><div className="grid gap-4 sm:grid-cols-3"><Select label="Напрям" value={draft.type} onChange={(value) => change("type", value as TaskDraft["type"])} values={taskTypes} labels={typeLabels}/><Select label="Статус" value={draft.status} onChange={(value) => change("status", value as TaskDraft["status"])} values={taskStatuses} labels={statusLabels}/><Select label="Пріоритет" value={draft.priority} onChange={(value) => change("priority", value as TaskDraft["priority"])} values={taskPriorities} labels={{ low: "Низький", normal: "Звичайний", high: "Високий" }}/></div><label className="label">Виконавець <span className="font-normal text-slate-500">(необовʼязково)</span><input value={draft.assigneeId} onChange={(e) => change("assigneeId", e.target.value)} className="field mt-1.5" placeholder="Ідентифікатор користувача"/></label></div><div className="mt-7 flex justify-end gap-3"><button type="button" onClick={onClose} className="btn-secondary">Скасувати</button><button disabled={saving} className="btn-primary">{saving ? "Зберігаємо..." : task ? "Зберегти зміни" : "Створити задачу"}</button></div></form></div>;
}

function Select({ label, value, onChange, values, labels }: { label: string; value: string; onChange: (value: string) => void; values: readonly string[]; labels: Record<string, string> }) { return <label className="label">{label}<select value={value} onChange={(e) => onChange(e.target.value)} className="field mt-1.5">{values.map((item) => <option value={item} key={item}>{labels[item]}</option>)}</select></label>; }

function AnalyticsPage() {
  const [stats, setStats] = useState<TaskTypeStat[]>([]); const [loading, setLoading] = useState(true); const [error, setError] = useState("");
  const load = async () => { setLoading(true); setError(""); try { setStats(await api.getTaskTypeStats()); } catch (e) { setError(messageOf(e)); } finally { setLoading(false); } };
  useEffect(() => { void load(); }, []);
  const total = stats.reduce((sum, item) => sum + item.todo + item.inProgress + item.done, 0);
  return <section><div className="mb-8"><p className="mb-2 text-sm font-medium text-lime-300">ОГЛЯД ПРОЦЕСУ</p><h1 className="text-3xl font-semibold tracking-tight sm:text-4xl">Аналітика за напрямами</h1><p className="mt-2 text-slate-400">Агреговані дані з вашого Analytics API.</p></div>{error && <Notice tone="error" text={error} action={<button onClick={() => void load()} className="underline">Оновити</button>}/>} {loading ? <div className="h-80 animate-pulse rounded-2xl bg-white/5"/> : <><div className="mb-6 grid gap-4 sm:grid-cols-3"><Metric label="Усього задач" value={total}/><Metric label="У роботі" value={stats.reduce((sum, item) => sum + item.inProgress, 0)}/><Metric label="Завершено" value={stats.reduce((sum, item) => sum + item.done, 0)}/></div><div className="overflow-hidden rounded-2xl border border-white/10 bg-slate-900"><div className="border-b border-white/10 px-6 py-5"><h2 className="font-semibold">Розподіл роботи</h2></div>{stats.length === 0 ? <p className="px-6 py-16 text-center text-slate-400">Дані ще не зʼявилися. Створіть першу задачу.</p> : <div className="divide-y divide-white/10">{stats.map((item) => { const value = item.todo + item.inProgress + item.done; return <div key={item.taskType} className="grid items-center gap-3 px-6 py-5 md:grid-cols-[150px_1fr_140px]"><span className="font-medium">{typeLabels[item.taskType]}</span><div className="flex h-3 overflow-hidden rounded-full bg-slate-800"><span className="bg-slate-500" style={{ width: `${value ? item.todo / value * 100 : 0}%` }}/><span className="bg-amber-300" style={{ width: `${value ? item.inProgress / value * 100 : 0}%` }}/><span className="bg-lime-300" style={{ width: `${value ? item.done / value * 100 : 0}%` }}/></div><span className="text-sm text-slate-400">{item.todo} план · {item.inProgress} роб. · {item.done} готово</span></div>; })}</div>}</div></>}</section>;
}

function Metric({ label, value }: { label: string; value: number }) { return <div className="rounded-2xl border border-white/10 bg-white/[0.035] p-5"><p className="text-sm text-slate-400">{label}</p><p className="mt-2 text-3xl font-semibold text-lime-300">{value}</p></div>; }
function Notice({ tone, text, action }: { tone: "error"; text: string; action?: ReactNode }) { return <div className={`mb-5 rounded-xl border px-4 py-3 text-sm ${tone === "error" ? "border-rose-400/30 bg-rose-400/10 text-rose-100" : ""}`}>{text} {action}</div>; }
function LoadingBoard() { return <div className="grid gap-5 xl:grid-cols-3">{taskStatuses.map((status) => <div key={status} className="h-72 animate-pulse rounded-2xl bg-white/5"/>)}</div>; }
function messageOf(error: unknown) { return error instanceof Error ? error.message : "Сталася неочікувана помилка."; }
