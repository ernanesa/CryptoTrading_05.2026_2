import type { MetricsSnapshot, IntelligenceSnapshot, AdaptiveRecommendation, HardeningReport, RuntimeStatus } from './types';

const API_BASE_URL = 'http://localhost:5020';

export const apiService = {
  fetchMetrics: async (): Promise<MetricsSnapshot> => {
    const res = await fetch(`${API_BASE_URL}/api/metrics`);
    if (!res.ok) throw new Error('Failed to fetch metrics');
    return res.json();
  },
  fetchIntelligence: async (): Promise<IntelligenceSnapshot> => {
    const res = await fetch(`${API_BASE_URL}/api/intelligence/snapshot?symbol=BTCUSDT&interval=1m&windowHours=48`);
    if (!res.ok) throw new Error('Failed to fetch intelligence');
    return res.json();
  },
  fetchAdaptive: async (): Promise<AdaptiveRecommendation> => {
    const res = await fetch(`${API_BASE_URL}/api/adaptive/recommendation?symbol=BTCUSDT&interval=1m&currentStrategyName=RSI%20Mean%20Reversion&persistentAdvantageCycles=2&windowHours=48&portfolioValue=10000`);
    if (!res.ok) throw new Error('Failed to fetch adaptive recommendation');
    return res.json();
  },
  fetchHardening: async (): Promise<HardeningReport> => {
    const res = await fetch(`${API_BASE_URL}/api/hardening/report`);
    if (!res.ok) throw new Error('Failed to fetch hardening report');
    return res.json();
  },
  fetchRuntimeStatus: async (): Promise<RuntimeStatus> => {
    const res = await fetch(`${API_BASE_URL}/api/runtime/status`);
    if (!res.ok) throw new Error('Failed to fetch runtime status');
    return res.json();
  }
};
