export const API_BASE_URL = 'http://localhost:5020';

export async function fetchMetrics() {
  const res = await fetch(`${API_BASE_URL}/api/metrics`);
  if (!res.ok) throw new Error('API Metrics fetch failed');
  return res.json();
}

export async function fetchIntelligence(symbol: string = 'BTCUSDT', interval: string = '1m') {
  const res = await fetch(`${API_BASE_URL}/api/intelligence/snapshot?symbol=${symbol}&interval=${interval}&windowHours=48`);
  if (!res.ok) throw new Error('API Intelligence fetch failed');
  return res.json();
}

export async function fetchAdaptive(symbol: string = 'BTCUSDT', interval: string = '1m') {
  const res = await fetch(`${API_BASE_URL}/api/adaptive/recommendation?symbol=${symbol}&interval=${interval}&currentStrategyName=RSI%20Mean%20Reversion&persistentAdvantageCycles=2&windowHours=48&portfolioValue=10000`);
  if (!res.ok) throw new Error('API Adaptive fetch failed');
  return res.json();
}

export async function fetchHardening() {
  const res = await fetch(`${API_BASE_URL}/api/hardening/report`);
  if (!res.ok) throw new Error('API Hardening fetch failed');
  return res.json();
}

export async function fetchBacktestRun(strategy: string, symbol: string, interval: string) {
  const startTime = new Date();
  startTime.setDate(startTime.getDate() - 7);
  const endTime = new Date();
  const url = `${API_BASE_URL}/api/backtest/run?strategyName=${strategy}&symbol=${symbol}&interval=${interval}&startTime=${startTime.toISOString()}&endTime=${endTime.toISOString()}`;
  const res = await fetch(url);
  if (!res.ok) throw new Error('API Backtest fetch failed');
  return res.json();
}
