'use client';

import { useState } from 'react';

type ConvertResponse = {
  message?: string;
  markdown?: string;
  detail?: string;
  title?: string;
};

const SUPPORTED_FORMATS = [
  { extension: '.docx', label: 'Word', note: 'Text and paragraph extraction' },
  { extension: '.xlsx', label: 'Excel', note: 'AI-friendly markdown tables' },
  { extension: '.pptx', label: 'PowerPoint', note: 'Slide outlines and bullets' },
  { extension: '.pdf', label: 'PDF', note: 'Page text extraction' },
];

const ACCEPTED_FILE_TYPES = [
  '.docx',
  '.xlsx',
  '.pptx',
  '.pdf',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  'application/vnd.openxmlformats-officedocument.presentationml.presentation',
  'application/pdf',
].join(',');

export default function Home() {
  const [file, setFile] = useState<File | null>(null);
  const [markdown, setMarkdown] = useState<string>('');
  const [error, setError] = useState<string>('');
  const [isUploading, setIsUploading] = useState(false);

  const getMarkdownFileName = (fileName: string) => {
    const baseName = fileName.replace(/\.[^.]+$/, '') || 'output';
    return `${baseName}.md`;
  };

  const downloadName = file ? getMarkdownFileName(file.name) : 'output.md';

  const formatBytes = (bytes: number) => {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  };

  const handleDownload = () => {
    if (!file || !markdown) return;

    const blob = new Blob([markdown], { type: 'text/markdown;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');

    link.href = url;
    link.download = downloadName;
    link.rel = 'noopener';
    document.body.appendChild(link);
    link.click();
    link.remove();

    URL.revokeObjectURL(url);
  };

  const handleUpload = async () => {
    if (!file || isUploading) return;

    const formData = new FormData();
    formData.append('file', file);

    setIsUploading(true);
    setMarkdown('');
    setError('');

    try {
      const res = await fetch('/api/convert', {
        method: 'POST',
        body: formData,
      });

      const contentType = res.headers.get('content-type') ?? '';
      const data = contentType.includes('application/json') || contentType.includes('+json')
        ? (await res.json()) as ConvertResponse
        : await res.text();

      if (!res.ok) {
        const message =
          typeof data === 'string'
            ? data
            : data.detail ?? data.message ?? data.title ?? 'Upload failed. Please try again.';
        throw new Error(message);
      }

      const markdown =
        typeof data === 'string'
          ? data
          : data.markdown ?? data.message ?? 'Conversion complete.';
      setMarkdown(markdown);
    } catch (error) {
      setError(
        error instanceof Error
          ? error.message
          : 'Upload failed. Check the console and ensure the backend is running.',
      );
    } finally {
      setIsUploading(false);
    }
  };

  return (
    <main className="flex min-h-screen flex-col items-center justify-center bg-gradient-to-br from-slate-950 via-slate-900 to-slate-800 p-6 text-white">
      <div className="w-full max-w-2xl rounded-2xl border border-white/10 bg-white/5 p-8 shadow-2xl shadow-black/30 backdrop-blur">
        <div className="mb-6 flex flex-col gap-2">
          <p className="text-sm uppercase tracking-[0.3em] text-amber-300/80">DocuMark</p>
          <h1 className="text-3xl font-bold tracking-tight">Office files to Markdown</h1>
          <p className="text-sm text-slate-300">
            Upload a .docx, .xlsx, .pptx, or .pdf file and the frontend will proxy it to the .NET backend.
          </p>
        </div>

        <div className="flex flex-col gap-4">
          <input
            type="file"
            accept={ACCEPTED_FILE_TYPES}
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            className="rounded-lg border border-white/10 bg-slate-950/70 px-4 py-3 text-sm text-slate-200 file:mr-4 file:rounded-md file:border-0 file:bg-amber-500 file:px-4 file:py-2 file:text-sm file:font-semibold file:text-white hover:file:bg-amber-400"
          />

          <button
            onClick={handleUpload}
            disabled={!file || isUploading}
            className="rounded-lg bg-amber-500 px-4 py-3 font-semibold text-slate-950 transition hover:bg-amber-400 disabled:cursor-not-allowed disabled:bg-slate-600 disabled:text-slate-300"
          >
            {isUploading ? 'Converting...' : 'Convert to Markdown'}
          </button>

          <div className="grid gap-2 sm:grid-cols-2">
            {SUPPORTED_FORMATS.map((format) => (
              <div
                key={format.extension}
                className="rounded-xl border border-white/10 bg-slate-950/50 px-4 py-3"
              >
                <div className="flex items-center justify-between gap-3">
                  <span className="text-sm font-semibold text-white">{format.label}</span>
                  <span className="rounded-full border border-white/10 px-2 py-0.5 text-[10px] uppercase tracking-[0.22em] text-slate-400">
                    {format.extension}
                  </span>
                </div>
                <p className="mt-1 text-xs text-slate-400">{format.note}</p>
              </div>
            ))}
          </div>
        </div>

        {markdown && (
          <div className="mt-6 rounded-2xl border border-emerald-400/20 bg-emerald-400/5 p-4 shadow-lg shadow-black/10">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex min-w-0 items-start gap-4">
                <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-emerald-400/10 text-emerald-200">
                  <svg width="22" height="22" viewBox="0 0 24 24" fill="none" aria-hidden="true">
                    <path d="M12 3v10m0 0 3.5-3.5M12 13l-3.5-3.5M5 15v3a1 1 0 0 0 1 1h12a1 1 0 0 0 1-1v-3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
                  </svg>
                </div>
                <div className="min-w-0">
                  <p className="text-xs uppercase tracking-[0.25em] text-emerald-300/80">Ready to download</p>
                  <h2 className="truncate text-lg font-semibold text-white">{downloadName}</h2>
                  <p className="mt-1 text-sm text-slate-300">
                    The downloaded file includes a conversion log header plus the extracted Markdown content.
                  </p>
                  <p className="mt-1 text-xs text-slate-400">
                    Source: {file?.name} {file ? `• ${formatBytes(file.size)}` : ''}
                  </p>
                </div>
              </div>
              <button
                onClick={handleDownload}
                className="inline-flex items-center justify-center rounded-xl bg-emerald-300 px-4 py-3 text-sm font-semibold text-slate-950 transition hover:bg-emerald-200"
              >
                Download Markdown
              </button>
            </div>
          </div>
        )}

        {error && (
          <div className="mt-6 rounded-xl border border-red-400/20 bg-red-400/10 p-4 text-sm text-red-200">
            {error}
          </div>
        )}

        {markdown && (
          <pre className="mt-6 max-h-[28rem] overflow-auto whitespace-pre-wrap rounded-xl border border-emerald-400/20 bg-black/40 p-4 text-sm leading-6 text-emerald-200">
            {markdown}
          </pre>
        )}
      </div>
    </main>
  );
}