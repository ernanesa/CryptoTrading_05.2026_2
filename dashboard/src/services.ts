import type { MetricsSnapshot, IntelligenceSnapshot, AdaptiveRecommendation, HardeningReport, RuntimeStatus } from './types';

const API_BASE_URL = 'http://localhost:5020';

const secureFetch = async (url: string, options: RequestInit = {}): Promise<Response> => {
  const headers = new Headers(options.headers);
  const token = localStorage.getItem('auth_token');
  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }
  
  try {
    const res = await fetch(url, { ...options, headers });
    if (res.status === 401) {
      localStorage.removeItem('auth_token');
      window.dispatchEvent(new Event('auth-failed'));
      throw new Error('Unauthorized');
    }
    return res;
  } catch (error) {
    if (error instanceof Error && error.message === 'Unauthorized') {
      throw error;
    }
    // Fallback para conexões perdidas ou erros genéricos
    throw error;
  }
};

export const apiService = {
  login: async (username: string, password: string): Promise<string> => {
    const res = await fetch(`${API_BASE_URL}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });
    
    if (!res.ok) {
      throw new Error('Credenciais inválidas.');
    }
    
    const data = await res.json();
    localStorage.setItem('auth_token', data.token);
    window.dispatchEvent(new Event('auth-success'));
    return data.token;
  },
  
  logout: () => {
    localStorage.removeItem('auth_token');
    window.dispatchEvent(new Event('auth-failed'));
  },
  
  isAuthenticated: (): boolean => {
    return !!localStorage.getItem('auth_token');
  },
  
  fetchMetrics: async (): Promise<MetricsSnapshot> => {
    const res = await secureFetch(`${API_BASE_URL}/api/metrics`);
    if (!res.ok) throw new Error('Failed to fetch metrics');
    return res.json();
  },
  
  fetchIntelligence: async (): Promise<IntelligenceSnapshot> => {
    const res = await secureFetch(`${API_BASE_URL}/api/intelligence/snapshot?symbol=BTCUSDT&interval=1m&windowHours=48`);
    if (!res.ok) throw new Error('Failed to fetch intelligence');
    return res.json();
  },
  
  fetchAdaptive: async (): Promise<AdaptiveRecommendation> => {
    const res = await secureFetch(`${API_BASE_URL}/api/adaptive/recommendation?symbol=BTCUSDT&interval=1m&currentStrategyName=RSI%20Mean%20Reversion&persistentAdvantageCycles=2&windowHours=48&portfolioValue=10000`);
    if (!res.ok) throw new Error('Failed to fetch adaptive recommendation');
    return res.json();
  },
  
  fetchHardening: async (): Promise<HardeningReport> => {
    const res = await secureFetch(`${API_BASE_URL}/api/hardening/report`);
    if (!res.ok) throw new Error('Failed to fetch hardening report');
    return res.json();
  },
  
  fetchRuntimeStatus: async (): Promise<RuntimeStatus> => {
    const res = await secureFetch(`${API_BASE_URL}/api/runtime/status`);
    if (!res.ok) throw new Error('Failed to fetch runtime status');
    return res.json();
  }
};
export { API_BASE_URL };
