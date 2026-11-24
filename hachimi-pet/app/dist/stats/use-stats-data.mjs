// The overview and the model panel each keep their last successful response.
export function useStatsData(React, { open, port, model, preset, start, end, tab, page }) {
  const view = model ? 'detail' : 'overview';
  const params = new URLSearchParams({ preset });
  if (preset === 'custom') {
    if (start) params.set('start', start);
    if (end) params.set('end', end);
  }
  if (model) params.set('model', model);
  const scope = `http://127.0.0.1:${port}/api/stats${model ? '/model' : ''}?${params}`;
  const url = model ? `${scope}&${new URLSearchParams({ tab, page, pageSize: 10 })}` : scope;

  const [results, setResults] = React.useState({});
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState('');
  const [revision, refresh] = React.useReducer(n => n + 1, 0);

  React.useEffect(() => {
    if (!open) return;
    const controller = new AbortController();
    const { signal } = controller;
    setLoading(true);
    setError('');
    async function load() {
      try {
        const response = await fetch(url, { signal });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const data = await response.json();
        if (!signal.aborted) setResults(previous => ({ ...previous, [view]: { scope, url, data } }));
      } catch (err) {
        if (!signal.aborted) setError(err instanceof Error ? err.message : '读取统计失败');
      } finally {
        if (!signal.aborted) setLoading(false);
      }
    }
    load();
    return () => controller.abort();
  }, [open, view, scope, url, revision]);

  const result = results[view];
  return {
    // Totals are shared within one model and range; table rows belong to one tab and page.
    data: result?.scope === scope ? result.data : null,
    rowsReady: result?.url === url,
    loading, error, refresh,
  };
}
