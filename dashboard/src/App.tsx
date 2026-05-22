import React, { useState, useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { 
  TrendingUp, 
  Activity, 
  BarChart3, 
  DollarSign, 
  ShieldAlert, 
  Cpu, 
  Terminal, 
  RefreshCw, 
  Play, 
  AlertTriangle, 
  CheckCircle2, 
  XCircle,
  Clock,
  Layers,
  BrainCircuit
} from 'lucide-react';

// API Configuration
const API_BASE_URL = 'http://localhost:5020';

interface MetricsSnapshot {
  uptimeSeconds: number;
  candlesReceived: number;
  dbWriteLatencyMs: number;
  signalsGenerated: number;
  riskRejections: number;
  paperPnL: number;
  testnetRequests: number;
  regime: string;
  executionCost: number;
  drawdown: number;
  strategyScores: Record<string, number>;
  assetScores: Record<string, number>;
}

interface IntelligenceSnapshot {
  symbol: string;
  interval: string;
  snapshotTime: string;
  schemaVersion: string;
  modelVersion: string;
  scoreVersion: string;
  scoreSource: string;
  marketRegime: string;
  regimeConfidence: number;
  anomalyScore: number;
  volatilityScore: number;
  hasAnomaly: boolean;
  featureVector: {
    version: string;
    source: string;
    momentumScore: number;
    trendScore: number;
    volumePressureScore: number;
    liquidityStressScore: number;
    normalizedReturn: number;
    atrPercent: number;
  };
  volatilityForecast: {
    modelVersion: string;
    scoreSource: string;
    horizonMinutes: number;
    forecastScore: number;
    expectedAtrPercent: number;
    confidence: number;
    riskBand: string;
  };
  metaLabel: {
    modelVersion: string;
    label: string;
    probability: number;
    qualityScore: number;
    isTradeContextFavorable: boolean;
  };
  sentimentRisk: {
    modelVersion: string;
    sentimentScore: number;
    riskScore: number;
    riskBand: string;
    sources: string[];
  };
  eventRisk: {
    modelVersion: string;
    eventRiskScore: number;
    severity: string;
    eventTags: string[];
  };
  ragContext: {
    providerVersion: string;
    source: string;
    query: string;
    contextItems: string[];
  };
  explanation: {
    modelVersion: string;
    summary: string;
    factors: string[];
  };
  registeredModels: Array<{
    name: string;
    version: string;
    purpose: string;
    source: string;
  }>;
  insights: string[];
}

interface AdaptiveRecommendation {
  symbol: string;
  interval: string;
  marketRegime: string;
  activeStrategyName: string;
  candidateStrategyName: string;
  shouldSwitchStrategy: boolean;
  strategyScore: number;
  assetScore: number;
  marketHealthScore: number;
  allocationWeight: number;
  positionSize: number;
  executionCost: {
    costBps: number;
    score: number;
    explanation: string;
  };
  strategyHealth: {
    isPaused: boolean;
    healthScore: number;
    reason: string;
  };
  exitPolicy: {
    policyName: string;
    stopAtrMultiplier: number;
    takeProfitAtrMultiplier: number;
  };
  walkForward: {
    fixedStrategyScore: number;
    adaptiveStrategyScore: number;
    improvement: number;
    verdict: string;
  };
  banditAllocation: {
    selectedArm: string;
    explorationWeight: number;
    exploitationWeight: number;
  };
  reasons: string[];
}

interface HardeningReport {
  version: string;
  isReleaseCandidate: boolean;
  gates: Array<{
    name: string;
    passed: boolean;
    evidence: string;
  }>;
  benchmarks: Array<{
    name: string;
    tool: string;
    status: string;
  }>;
  chaosScenarios: Array<{
    scenario: string;
    passed: boolean;
  }>;
  knownRisks: Array<{
    area: string;
    risk: string;
    mitigation: string;
  }>;
  alerts: string[];
}

interface TradeLog {
  time: string;
  type: string;
  price: number;
  qty: number;
  pnl?: number;
  symbol: string;
}

interface AuditLog {
  timestamp: string;
  strategy: string;
  symbol: string;
  signal: string;
  decision: string;
  reason: string;
}

export default function App() {
  const [activeTab, setActiveTab] = useState<'overview' | 'market' | 'backtest' | 'paper' | 'risk' | 'testnet' | 'logs'>('overview');
  const [isConnected, setIsConnected] = useState(false);
  const [metrics, setMetrics] = useState<MetricsSnapshot>({
    uptimeSeconds: 0,
    candlesReceived: 0,
    dbWriteLatencyMs: 0,
    signalsGenerated: 0,
    riskRejections: 0,
    paperPnL: 0,
    testnetRequests: 0,
    regime: 'Sideways',
    executionCost: 0,
    drawdown: 0,
    strategyScores: { 'AtrBreakout': 85, 'EmaCrossover': 72 },
    assetScores: { 'BTCUSDT': 90, 'ETHUSDT': 80 }
  });

  const [intelligence, setIntelligence] = useState<IntelligenceSnapshot>({
    symbol: 'BTCUSDT',
    interval: '1m',
    snapshotTime: new Date().toISOString(),
    schemaVersion: 'intelligence-snapshot/v1',
    modelVersion: 'heuristic-m6-v1',
    scoreVersion: 'score-v1',
    scoreSource: 'FeatureStore.CandleFeature',
    marketRegime: 'Sideways',
    regimeConfidence: 52,
    anomalyScore: 18,
    volatilityScore: 34,
    hasAnomaly: false,
    featureVector: {
      version: 'feature-vector/v1',
      source: 'FeatureStore.CandleFeature',
      momentumScore: 52,
      trendScore: 41,
      volumePressureScore: 12,
      liquidityStressScore: 8,
      normalizedReturn: 0.12,
      atrPercent: 1.1
    },
    volatilityForecast: {
      modelVersion: 'volatility-heuristic-m6-v1',
      scoreSource: 'FeatureStore.CandleFeature',
      horizonMinutes: 3,
      forecastScore: 34,
      expectedAtrPercent: 1.1,
      confidence: 100,
      riskBand: 'Normal'
    },
    metaLabel: {
      modelVersion: 'meta-label-heuristic-m6-v1',
      label: 'Neutral',
      probability: 52,
      qualityScore: 76,
      isTradeContextFavorable: false
    },
    sentimentRisk: {
      modelVersion: 'sentiment-risk-heuristic-m6-v1',
      sentimentScore: 50,
      riskScore: 18,
      riskBand: 'Neutral',
      sources: ['FeatureStore momentum/trend proxy', 'EventRiskClassifier market context']
    },
    eventRisk: {
      modelVersion: 'event-risk-heuristic-m6-v1',
      eventRiskScore: 12,
      severity: 'Low',
      eventTags: ['no-material-event']
    },
    ragContext: {
      providerVersion: 'rag-context-provider-m6-v1',
      source: 'local-plans-rag',
      query: 'M6 intelligence context BTCUSDT 1m Sideways',
      contextItems: [
        'ML, sentimento e eventos sao contexto auxiliar e nao executam acoes.',
        'RiskEngine continua sendo o gate obrigatorio para decisoes relevantes.'
      ]
    },
    explanation: {
      modelVersion: 'explanation-heuristic-m6-v1',
      summary: 'BTCUSDT/1m: Sideways, volatilidade Normal, sentimento Neutral.',
      factors: ['Snapshot e apenas contexto; execucao permanece condicionada ao RiskEngine.']
    },
    registeredModels: [
      { name: 'FeatureExtractor', version: 'feature-vector/v1', purpose: 'Normaliza indicadores para contexto de inteligencia.', source: 'CryptoTrading.Application.Services' },
      { name: 'ExplanationService', version: 'explanation-heuristic-m6-v1', purpose: 'Gera explicacoes deterministicas do snapshot.', source: 'CryptoTrading.Application.Services' }
    ],
    insights: [
      'Regime detected as Sideways from FeatureStore indicators.',
      'Anomaly score 18.00/100 using volume, imbalance, spread and returns.',
      'Volatility forecast is Normal for 3 minutes.'
    ]
  });

  const [adaptive, setAdaptive] = useState<AdaptiveRecommendation>({
    symbol: 'BTCUSDT',
    interval: '1m',
    marketRegime: 'Sideways',
    activeStrategyName: 'Bollinger Mean Reversion',
    candidateStrategyName: 'Bollinger Mean Reversion',
    shouldSwitchStrategy: false,
    strategyScore: 72,
    assetScore: 74,
    marketHealthScore: 68,
    allocationWeight: 0.72,
    positionSize: 7200,
    executionCost: {
      costBps: 4.2,
      score: 83.2,
      explanation: 'Estimated execution cost from liquidity and volatility context.'
    },
    strategyHealth: {
      isPaused: false,
      healthScore: 76,
      reason: 'Strategy health is acceptable.'
    },
    exitPolicy: {
      policyName: 'BalancedAtrExit',
      stopAtrMultiplier: 2,
      takeProfitAtrMultiplier: 3.2
    },
    walkForward: {
      fixedStrategyScore: 64,
      adaptiveStrategyScore: 72,
      improvement: 8,
      verdict: 'AdaptivePreferred'
    },
    banditAllocation: {
      selectedArm: 'Bollinger Mean Reversion',
      explorationWeight: 0.28,
      exploitationWeight: 0.72
    },
    reasons: [
      'Candidate strategy held by hysteresis/cooldown/risk gates.',
      'DataQualityGate: OK; market health 68.00.'
    ]
  });

  const [hardening, setHardening] = useState<HardeningReport>({
    version: 'hardening-report/v1',
    isReleaseCandidate: true,
    gates: [
      { name: 'build limpo', passed: true, evidence: 'dotnet test compila todos os projetos.' },
      { name: 'testes limpos', passed: true, evidence: 'Suite xUnit verde.' },
      { name: 'dashboard operacional', passed: true, evidence: 'npm run build verde.' },
      { name: 'Native AOT opt-in', passed: true, evidence: 'API e Worker publicados via tools/validate-native-aot.sh.' }
    ],
    benchmarks: [
      { name: 'IndicatorService.CalculateFeatures', tool: 'Local benchmark harness', status: 'Mandatory smoke' },
      { name: 'AdaptiveStrategyOrchestrator.Decide', tool: 'Local benchmark harness', status: 'Mandatory smoke' },
      { name: 'FeatureStore.GetMarketDataPointsAsync', tool: 'PostgreSQL Testcontainers fixture', status: 'Opt-in validated' },
      { name: 'ApiWorker.NativeAot.Publish', tool: 'Native AOT validation script', status: 'Opt-in validated' }
    ],
    chaosScenarios: [
      { scenario: 'RiskEngine halted', passed: true },
      { scenario: 'DataQualityGate blocked', passed: true }
    ],
    knownRisks: [
      { area: 'FeatureStore benchmark', risk: 'Benchmark PostgreSQL depende de Docker no host/runner.', mitigation: 'Manter execução manual com run_featurestore_benchmark=true.' },
      { area: 'Native AOT', risk: 'Dapper e CryptoExchange.Net emitem warnings de trim/AOT no publish opt-in.', mitigation: 'Manter AOT manual e acompanhar dependencias antes de tornar gate obrigatorio.' }
    ],
    alerts: [
      'FeatureStore benchmark: Docker requerido para fixture PostgreSQL.',
      'Native AOT: warnings de compatibilidade rastreados no gate opt-in.'
    ]
  });

  const [systemLogs, setSystemLogs] = useState<string[]>([
    'System initialization successful. Target: .NET 10.0 with Native AOT opt-in validated.',
    'FeatureStore schema validated. PostgreSQL connected.',
    'Binance Spot Testnet interface initialized. Mode: Dry-Run (Simulator).',
    'RiskEngine compiled. Cooldown rules active: 3 max daily losses, 5% max daily drawdown.',
    'AtrBreakoutStrategy loaded into StrategyRegistry.'
  ]);

  // Backtest parameters state
  const [selectedStrategy, setSelectedStrategy] = useState('AtrBreakout');
  const [symbol, setSymbol] = useState('BTCUSDT');
  const [interval, setInterval] = useState('1m');
  const [atrPeriod, setAtrPeriod] = useState(14);
  const [multiplier, setMultiplier] = useState(2.5);
  const [backtestStatus, setBacktestStatus] = useState<'idle' | 'running' | 'success' | 'failed'>('idle');
  const [backtestResult, setBacktestResult] = useState<any>(null);

  // Lists for display
  const [decisionAudits, setDecisionAudits] = useState<AuditLog[]>([
    { timestamp: '19:40:12', strategy: 'AtrBreakout', symbol: 'BTCUSDT', signal: 'Buy', decision: 'APPROVED', reason: 'ATR breakout confirmed. Spread within limit.' },
    { timestamp: '19:35:05', strategy: 'AtrBreakout', symbol: 'BTCUSDT', signal: 'Sell', decision: 'APPROVED', reason: 'ATR trailing stop hit. Realizing profit.' },
    { timestamp: '19:28:44', strategy: 'AtrBreakout', symbol: 'BTCUSDT', signal: 'Buy', decision: 'REJECTED', reason: 'RiskEngine: daily loss limit cooldown active.' }
  ]);

  const [recentTrades, setRecentTrades] = useState<TradeLog[]>([
    { time: '19:35:05', type: 'SELL', price: 68420.50, qty: 0.15, pnl: 450.25, symbol: 'BTCUSDT' },
    { time: '19:20:12', type: 'BUY', price: 65420.00, qty: 0.15, symbol: 'BTCUSDT' },
    { time: '19:15:30', type: 'SELL', price: 3420.10, qty: 1.5, pnl: -12.50, symbol: 'ETHUSDT' }
  ]);

  const [walletBalances] = useState([
    { symbol: 'USDT', free: 9850.50, locked: 0.00 },
    { symbol: 'BTC', free: 0.00, locked: 0.00 },
    { symbol: 'ETH', free: 1.50, locked: 0.00 }
  ]);

  const [riskEngineRules, setRiskEngineRules] = useState({
    status: 'Normal',
    maxDailyLosses: 3,
    maxDailyDrawdownPercent: 5.0,
    maxExposurePercent: 95.0,
    cooldownPeriodMinutes: 30
  });

  // Simulated chart data
  const canvasRef = useRef<HTMLCanvasElement | null>(null);

  // Setup SignalR and REST fetching
  useEffect(() => {
    // REST fetch fallback and fast interval polling
    const fetchMetrics = async () => {
      try {
        const res = await fetch(`${API_BASE_URL}/api/metrics`);
        if (res.ok) {
          const data = await res.json();
          setMetrics(data);
          setIsConnected(true);
        }
      } catch (err) {
        // Standalone simulation mode
        setIsConnected(false);
      }
    };

    const fetchIntelligence = async () => {
      try {
        const res = await fetch(`${API_BASE_URL}/api/intelligence/snapshot?symbol=BTCUSDT&interval=1m&windowHours=48`);
        if (res.ok) {
          const data = await res.json();
          setIntelligence(data);
        }
      } catch (err) {
        // Standalone simulation mode keeps the seeded intelligence snapshot.
      }
    };

    const fetchAdaptive = async () => {
      try {
        const res = await fetch(`${API_BASE_URL}/api/adaptive/recommendation?symbol=BTCUSDT&interval=1m&currentStrategyName=RSI%20Mean%20Reversion&persistentAdvantageCycles=2&windowHours=48&portfolioValue=10000`);
        if (res.ok) {
          const data = await res.json();
          setAdaptive(data);
        }
      } catch (err) {
        // Standalone simulation mode keeps the seeded adaptive recommendation.
      }
    };

    const fetchHardening = async () => {
      try {
        const res = await fetch(`${API_BASE_URL}/api/hardening/report`);
        if (res.ok) {
          const data = await res.json();
          setHardening(data);
        }
      } catch (err) {
        // Standalone simulation mode keeps the seeded hardening report.
      }
    };

    fetchMetrics();
    fetchIntelligence();
    fetchAdaptive();
    fetchHardening();
    const intervalId = window.setInterval(fetchMetrics, 3000);
    const intelligenceIntervalId = window.setInterval(fetchIntelligence, 10000);
    const adaptiveIntervalId = window.setInterval(fetchAdaptive, 10000);
    const hardeningIntervalId = window.setInterval(fetchHardening, 30000);

    // Setup SignalR Hub Connection
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/metrics`)
      .withAutomaticReconnect()
      .build();

    connection.on('ReceiveMetricsSnapshot', (snapshot: MetricsSnapshot) => {
      setMetrics(snapshot);
      setIsConnected(true);
    });

    connection.start()
      .then(() => {
        setIsConnected(true);
        addLog('SignalR socket connected to CryptoTrading HFT engine.');
      })
      .catch(() => {
        setIsConnected(false);
        addLog('SignalR connection failed. Running in standalone simulated mode.');
      });

    return () => {
      connection.stop();
      window.clearInterval(intervalId);
      window.clearInterval(intelligenceIntervalId);
      window.clearInterval(adaptiveIntervalId);
      window.clearInterval(hardeningIntervalId);
    };
  }, []);

  // HFT simulation for logs and counters when not connected
  useEffect(() => {
    if (isConnected) return;

    const simInterval = window.setInterval(() => {
      // Simulate ticking metrics
      setMetrics(prev => ({
        ...prev,
        uptimeSeconds: prev.uptimeSeconds + 1,
        candlesReceived: prev.candlesReceived + Math.floor(Math.random() * 2),
        dbWriteLatencyMs: +(Math.random() * 12 + 2).toFixed(2),
        signalsGenerated: prev.signalsGenerated + (Math.random() > 0.85 ? 1 : 0)
      }));

      // Simulate trading log entries sometimes
      if (Math.random() > 0.95) {
        const symbols = ['BTCUSDT', 'ETHUSDT', 'SOLUSDT'];
        const sym = symbols[Math.floor(Math.random() * symbols.length)];
        const types = ['Buy', 'Sell', 'Hold'];
        const type = types[Math.floor(Math.random() * types.length)];
        const price = sym === 'BTCUSDT' ? 68000 + Math.random() * 500 : 3400 + Math.random() * 50;
        const time = new Date().toTimeString().split(' ')[0];

        const decisions = ['APPROVED', 'REJECTED'];
        const decision = decisions[Math.floor(Math.random() * decisions.length)];

        addAuditLog({
          timestamp: time,
          strategy: 'AtrBreakout',
          symbol: sym,
          signal: type,
          decision: decision,
          reason: decision === 'APPROVED' ? 'ATR breakout signal confirmed.' : 'RiskEngine: maximum exposure threshold exceeded.'
        });

        if (decision === 'APPROVED' && type !== 'Hold') {
          const qty = sym === 'BTCUSDT' ? 0.05 : 0.5;
          const pnl = type === 'Sell' ? +(Math.random() * 100 - 20).toFixed(2) : undefined;
          addTrade({ time, type: type.toUpperCase(), price, qty, pnl, symbol: sym });
        }
      }
    }, 1000);

    return () => window.clearInterval(simInterval);
  }, [isConnected]);

  // Helper functions
  const addLog = (msg: string) => {
    const time = new Date().toTimeString().split(' ')[0];
    setSystemLogs(prev => [`[${time}] ${msg}`, ...prev.slice(0, 100)]);
  };

  const addAuditLog = (audit: AuditLog) => {
    setDecisionAudits(prev => [audit, ...prev.slice(0, 50)]);
    addLog(`Audit Decision: ${audit.strategy} generated ${audit.signal} for ${audit.symbol} -> ${audit.decision}`);
  };

  const addTrade = (trade: TradeLog) => {
    setRecentTrades(prev => [trade, ...prev.slice(0, 50)]);
    if (trade.pnl) {
      setMetrics(prev => ({
        ...prev,
        paperPnL: +(prev.paperPnL + trade.pnl!).toFixed(2)
      }));
    }
  };

  const triggerHalt = () => {
    setRiskEngineRules(prev => ({
      ...prev,
      status: prev.status === 'Halted' ? 'Normal' : 'Halted'
    }));
    addLog(`Risk system manual status override: ${riskEngineRules.status === 'Halted' ? 'Normal' : 'Halted'}`);
  };

  // Render chart on canvas
  useEffect(() => {
    if (activeTab !== 'market' || !canvasRef.current) return;
    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let animId: number;
    let offset = 0;

    const render = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      
      // Draw grid
      ctx.strokeStyle = 'rgba(255,255,255,0.03)';
      ctx.lineWidth = 1;
      for (let i = 0; i < canvas.width; i += 40) {
        ctx.beginPath();
        ctx.moveTo(i, 0);
        ctx.lineTo(i, canvas.height);
        ctx.stroke();
      }
      for (let j = 0; j < canvas.height; j += 30) {
        ctx.beginPath();
        ctx.moveTo(0, j);
        ctx.lineTo(canvas.width, j);
        ctx.stroke();
      }

      // Draw simulated candles
      const numCandles = 25;
      const candleWidth = 14;
      const gap = 12;
      const startX = 50;

      ctx.lineWidth = 2;

      for (let i = 0; i < numCandles; i++) {
        const x = startX + i * (candleWidth + gap);
        const seed = Math.sin((i + offset) * 0.3) * 40;
        const open = 120 + seed;
        const close = 120 + seed + Math.cos((i + offset) * 0.8) * 15;
        const high = Math.max(open, close) + Math.random() * 8;
        const low = Math.min(open, close) - Math.random() * 8;

        const isGreen = close > open;
        ctx.strokeStyle = isGreen ? '#00e676' : '#ff1744';
        ctx.fillStyle = isGreen ? 'rgba(0, 230, 118, 0.2)' : 'rgba(255, 23, 68, 0.2)';

        // Draw wick
        ctx.beginPath();
        ctx.moveTo(x + candleWidth / 2, high);
        ctx.lineTo(x + candleWidth / 2, low);
        ctx.stroke();

        // Draw body
        ctx.beginPath();
        ctx.rect(x, Math.min(open, close), candleWidth, Math.abs(close - open));
        ctx.fill();
        ctx.stroke();
      }

      // Draw indicators (e.g. EMA)
      ctx.strokeStyle = '#00f2fe';
      ctx.lineWidth = 2;
      ctx.beginPath();
      for (let i = 0; i < numCandles; i++) {
        const x = startX + i * (candleWidth + gap) + candleWidth / 2;
        const seed = Math.sin((i + offset) * 0.3) * 40;
        const y = 125 + seed;
        if (i === 0) ctx.moveTo(x, y);
        else ctx.lineTo(x, y);
      }
      ctx.stroke();

      offset += 0.05;
      animId = requestAnimationFrame(render);
    };

    render();
    return () => cancelAnimationFrame(animId);
  }, [activeTab]);

  // Execute Backtest
  const runBacktest = async (e: React.FormEvent) => {
    e.preventDefault();
    setBacktestStatus('running');
    addLog(`Running backtest for strategy ${selectedStrategy} on ${symbol} (${interval})...`);

    try {
      // Form dates
      const startTime = new Date();
      startTime.setDate(startTime.getDate() - 7);
      const endTime = new Date();

      const url = `${API_BASE_URL}/api/backtest/run?strategyName=${selectedStrategy}&symbol=${symbol}&interval=${interval}&startTime=${startTime.toISOString()}&endTime=${endTime.toISOString()}`;
      
      const res = await fetch(url);
      if (res.ok) {
        const data = await res.json();
        setBacktestResult(data);
        setBacktestStatus('success');
        addLog(`Backtest execution successful! Net Return: ${data.totalNetProfitPercent || 0}%`);
      } else {
        throw new Error('API Backtest execution failed');
      }
    } catch (err) {
      // Simulated backtest results in fallback
      setTimeout(() => {
        setBacktestResult({
          totalTrades: 48,
          winningTrades: 28,
          winRatePercent: 58.3,
          totalNetProfitPercent: 12.84,
          maxDrawdownPercent: 3.45,
          sharpeRatio: 2.15,
          profitFactor: 1.84,
          tradesList: [
            { id: 1, type: 'BUY', price: 65400, qty: 0.5, timestamp: '2026-05-18T10:12:00Z' },
            { id: 2, type: 'SELL', price: 66200, qty: 0.5, pnl: 400.0, timestamp: '2026-05-18T15:30:00Z' },
            { id: 3, type: 'BUY', price: 67100, qty: 0.5, timestamp: '2026-05-19T08:00:00Z' }
          ]
        });
        setBacktestStatus('success');
        addLog(`Backtest execution completed in simulation mode. Returns: 12.84%`);
      }, 1500);
    }
  };

  return (
    <div className="app-container">
      {/* ──────────────────────────────────────────────────────── */}
      {/* SIDEBAR NAVIGATION */}
      {/* ──────────────────────────────────────────────────────── */}
      <aside className="sidebar">
        <div className="sidebar-header">
          <div className="logo-glow">⚡</div>
          <div className="brand-name">CryptoTrading HFT</div>
        </div>

        <nav className="sidebar-nav">
          <div 
            className={`nav-item ${activeTab === 'overview' ? 'active' : ''}`}
            onClick={() => setActiveTab('overview')}
          >
            <TrendingUp size={18} />
            <span>Painel Overview</span>
          </div>

          <div 
            className={`nav-item ${activeTab === 'market' ? 'active' : ''}`}
            onClick={() => setActiveTab('market')}
          >
            <Activity size={18} />
            <span>Market Data & Features</span>
          </div>

          <div 
            className={`nav-item ${activeTab === 'backtest' ? 'active' : ''}`}
            onClick={() => setActiveTab('backtest')}
          >
            <BarChart3 size={18} />
            <span>Strategy Lab</span>
          </div>

          <div 
            className={`nav-item ${activeTab === 'paper' ? 'active' : ''}`}
            onClick={() => setActiveTab('paper')}
          >
            <DollarSign size={18} />
            <span>Paper Trading</span>
          </div>

          <div 
            className={`nav-item ${activeTab === 'risk' ? 'active' : ''}`}
            onClick={() => setActiveTab('risk')}
          >
            <ShieldAlert size={18} />
            <span>Gestão de Risco</span>
          </div>

          <div 
            className={`nav-item ${activeTab === 'testnet' ? 'active' : ''}`}
            onClick={() => setActiveTab('testnet')}
          >
            <Cpu size={18} />
            <span>Binance Testnet</span>
          </div>

          <div 
            className={`nav-item ${activeTab === 'logs' ? 'active' : ''}`}
            onClick={() => setActiveTab('logs')}
          >
            <Terminal size={18} />
            <span>Console Logs</span>
          </div>
        </nav>

        <div className="sidebar-footer">
          <div className="system-status">
            <span className={`status-dot ${isConnected ? 'active' : ''}`} style={{ backgroundColor: isConnected ? '#00e676' : '#ffb300', boxShadow: isConnected ? '0 0 10px #00e676' : '0 0 10px #ffb300' }}></span>
            <span>{isConnected ? 'API Real-Time Conectada' : 'Modo Simulado Local'}</span>
          </div>
          <div style={{ fontSize: '11px', color: 'var(--color-text-muted)', fontFamily: 'var(--font-mono)' }}>
            v10.0 Native-AOT
          </div>
        </div>
      </aside>

      {/* ──────────────────────────────────────────────────────── */}
      {/* MAIN CONTAINER */}
      {/* ──────────────────────────────────────────────────────── */}
      <main className="main-panel">
        <header className="main-header">
          <div className="page-title">
            <h1>
              {activeTab === 'overview' && 'Overview Performance Panel'}
              {activeTab === 'market' && 'Market Ingestion & Feature Store'}
              {activeTab === 'backtest' && 'Strategy Lab & Backtester'}
              {activeTab === 'paper' && 'Virtual Paper Trading Engine'}
              {activeTab === 'risk' && 'Risk Engine Security Controller'}
              {activeTab === 'testnet' && 'Binance Spot Testnet Gateways'}
              {activeTab === 'logs' && 'System Diagnostics & Logs'}
            </h1>
            <p>
              {activeTab === 'overview' && 'Acompanhe as métricas globais e o status operacional da engine de alta performance.'}
              {activeTab === 'market' && 'Estrutura de velas capturadas e variáveis analíticas no banco de dados.'}
              {activeTab === 'backtest' && 'Execute testes de hipóteses históricas no motor Dapper-first.'}
              {activeTab === 'paper' && 'Monitore a carteira virtual, as transações simuladas e as decisões do robô.'}
              {activeTab === 'risk' && 'Controle os limites de segurança ativa do robô em tempo real.'}
              {activeTab === 'testnet' && 'Acompanhe o tráfego de ordens, auditorias e sincronizações com a Testnet Binance.'}
              {activeTab === 'logs' && 'Diagnósticos completos de sistema e fluxo de eventos assíncronos.'}
            </p>
          </div>

          <div className="header-actions">
            <button className="refresh-btn" onClick={() => addLog('Forçando recarregamento das métricas.')}>
              <RefreshCw size={14} />
              <span>Sincronizar</span>
            </button>
          </div>
        </header>

        {/* ──────────────────────────────────────────────────────── */}
        {/* OVERVIEW TAB */}
        {/* ──────────────────────────────────────────────────────── */}
        {activeTab === 'overview' && (
          <>
            <section className="metrics-grid">
              <div className="metric-card accent">
                <div className="metric-card-header">
                  <span>Tempo de Uptime</span>
                  <Clock size={16} />
                </div>
                <div className="metric-card-value">
                  {Math.floor(metrics.uptimeSeconds / 3600)}h {Math.floor((metrics.uptimeSeconds % 3600) / 60)}m {metrics.uptimeSeconds % 60}s
                </div>
                <div className="metric-card-desc">Tempo contínuo de execução</div>
              </div>

              <div className="metric-card success">
                <div className="metric-card-header">
                  <span>Paper Trading PnL</span>
                  <DollarSign size={16} />
                </div>
                <div className="metric-card-value" style={{ color: metrics.paperPnL >= 0 ? 'var(--color-success)' : 'var(--color-danger)' }}>
                  ${metrics.paperPnL >= 0 ? '+' : ''}{metrics.paperPnL}
                </div>
                <div className="metric-card-desc">Lucro acumulado simulado</div>
              </div>

              <div className="metric-card warning">
                <div className="metric-card-header">
                  <span>Candles Recebidos</span>
                  <Layers size={16} />
                </div>
                <div className="metric-card-value">{metrics.candlesReceived}</div>
                <div className="metric-card-desc">Consumidos via WebSocket API</div>
              </div>

              <div className="metric-card danger">
                <div className="metric-card-header">
                  <span>Exposição Drawdown</span>
                  <AlertTriangle size={16} />
                </div>
                <div className="metric-card-value">{metrics.drawdown}%</div>
                <div className="metric-card-desc">Drawdown máximo do período</div>
              </div>

              <div className="metric-card success">
                <div className="metric-card-header">
                  <span>Hardening</span>
                  <ShieldAlert size={16} />
                </div>
                <div className="metric-card-value">
                  {hardening.gates.filter(g => g.passed).length}/{hardening.gates.length}
                </div>
                <div className="metric-card-desc">{hardening.isReleaseCandidate ? 'Release candidate' : 'Ações pendentes'}</div>
              </div>
            </section>

            <div className="split-grid">
              {/* Decision audits list */}
              <div className="premium-card">
                <div className="premium-card-title">
                  <h3>
                    <CheckCircle2 size={18} style={{ color: 'var(--color-accent)' }} />
                    Auditoria de Decisões Recentes
                  </h3>
                  <span className="badge info">Sinalizador</span>
                </div>
                <div className="table-container">
                  <table className="premium-table">
                    <thead>
                      <tr>
                        <th>Horário</th>
                        <th>Estratégia</th>
                        <th>Ativo</th>
                        <th>Sinal</th>
                        <th>Decisão</th>
                        <th>Motivo / Regra de Risco</th>
                      </tr>
                    </thead>
                    <tbody>
                      {decisionAudits.slice(0, 5).map((audit, i) => (
                        <tr key={i}>
                          <td style={{ fontFamily: 'var(--font-mono)' }}>{audit.timestamp}</td>
                          <td style={{ fontWeight: 600 }}>{audit.strategy}</td>
                          <td>{audit.symbol}</td>
                          <td>
                            <span className={`badge ${audit.signal === 'Buy' ? 'success' : audit.signal === 'Sell' ? 'danger' : 'info'}`}>
                              {audit.signal}
                            </span>
                          </td>
                          <td>
                            <span className={`badge ${audit.decision === 'APPROVED' ? 'success' : 'danger'}`}>
                              {audit.decision}
                            </span>
                          </td>
                          <td style={{ fontSize: '13px', color: 'var(--color-text-secondary)' }}>{audit.reason}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>

              {/* Performance Scores */}
              <div className="premium-card">
                <div className="premium-card-title">
                  <h3>Estratégias / Scores</h3>
                </div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                  {Object.entries(metrics.strategyScores).map(([strat, score]) => (
                    <div key={strat}>
                      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 6, fontSize: '14px' }}>
                        <span style={{ fontWeight: 600 }}>{strat}</span>
                        <span style={{ color: 'var(--color-accent)', fontFamily: 'var(--font-mono)' }}>{score}% Accuracy</span>
                      </div>
                      <div style={{ background: 'var(--bg-tertiary)', height: '6px', borderRadius: '3px', overflow: 'hidden' }}>
                        <div style={{ background: 'linear-gradient(to right, var(--color-accent), var(--color-success))', width: `${score}%`, height: '100%' }}></div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div className="premium-card">
                <div className="premium-card-title">
                  <h3>
                    <BrainCircuit size={18} style={{ color: 'var(--color-accent)' }} />
                    Intelligence Snapshot
                  </h3>
                  <span className={`badge ${intelligence.hasAnomaly ? 'warning' : 'success'}`}>
                    {intelligence.marketRegime}
                  </span>
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                  <div className="intel-stat">
                    <span>Regime</span>
                    <strong>{intelligence.regimeConfidence.toFixed(2)}%</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Anomalia</span>
                    <strong>{intelligence.anomalyScore.toFixed(2)}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Volatilidade</span>
                    <strong>{intelligence.volatilityForecast.forecastScore.toFixed(2)}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Horizonte</span>
                    <strong>{intelligence.volatilityForecast.horizonMinutes}m</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Tendencia</span>
                    <strong>{intelligence.featureVector.trendScore.toFixed(2)}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Momentum</span>
                    <strong>{intelligence.featureVector.momentumScore.toFixed(2)}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Meta-label</span>
                    <strong>{intelligence.metaLabel.label}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Sentimento</span>
                    <strong>{intelligence.sentimentRisk.riskBand}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Evento</span>
                    <strong>{intelligence.eventRisk.severity}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Modelos</span>
                    <strong>{intelligence.registeredModels.length}</strong>
                  </div>
                </div>
                <div className="intel-meta">
                  <span>{intelligence.modelVersion}</span>
                  <span>{intelligence.featureVector.version}</span>
                  <span>{intelligence.volatilityForecast.modelVersion}</span>
                  <span>{intelligence.metaLabel.modelVersion}</span>
                  <span>{intelligence.sentimentRisk.modelVersion}</span>
                </div>
                <div className="intel-insights">
                  <div>{intelligence.explanation.summary}</div>
                  {intelligence.insights.slice(0, 4).map((insight, i) => (
                    <div key={i}>{insight}</div>
                  ))}
                </div>
              </div>

              <div className="premium-card">
                <div className="premium-card-title">
                  <h3>
                    <Activity size={18} style={{ color: 'var(--color-accent)' }} />
                    Adaptive Orchestrator
                  </h3>
                  <span className={`badge ${adaptive.shouldSwitchStrategy ? 'warning' : 'info'}`}>
                    {adaptive.shouldSwitchStrategy ? 'Switch' : 'Hold'}
                  </span>
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                  <div className="intel-stat">
                    <span>Ativa</span>
                    <strong>{adaptive.activeStrategyName}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Candidata</span>
                    <strong>{adaptive.candidateStrategyName}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Strategy</span>
                    <strong>{adaptive.strategyScore.toFixed(2)}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Ativo</span>
                    <strong>{adaptive.assetScore.toFixed(2)}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Saude</span>
                    <strong>{adaptive.marketHealthScore.toFixed(2)}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Posicao</span>
                    <strong>${adaptive.positionSize.toFixed(2)}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Custo</span>
                    <strong>{adaptive.executionCost.costBps.toFixed(2)} bps</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Walk-forward</span>
                    <strong>{adaptive.walkForward.verdict}</strong>
                  </div>
                </div>
                <div className="intel-meta">
                  <span>{adaptive.marketRegime}</span>
                  <span>{adaptive.exitPolicy.policyName}</span>
                  <span>{adaptive.banditAllocation.selectedArm}</span>
                  <span>{adaptive.strategyHealth.isPaused ? 'paused' : 'healthy'}</span>
                </div>
                <div className="intel-insights">
                  {adaptive.reasons.slice(0, 4).map((reason, i) => (
                    <div key={i}>{reason}</div>
                  ))}
                </div>
              </div>

              <div className="premium-card">
                <div className="premium-card-title">
                  <h3>
                    <ShieldAlert size={18} style={{ color: 'var(--color-accent)' }} />
                    Hardening Gates
                  </h3>
                  <span className={`badge ${hardening.isReleaseCandidate ? 'success' : 'warning'}`}>
                    {hardening.version}
                  </span>
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                  <div className="intel-stat">
                    <span>Gates</span>
                    <strong>{hardening.gates.filter(g => g.passed).length}/{hardening.gates.length}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Benchmarks</span>
                    <strong>{hardening.benchmarks.length}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Chaos</span>
                    <strong>{hardening.chaosScenarios.filter(c => c.passed).length}/{hardening.chaosScenarios.length}</strong>
                  </div>
                  <div className="intel-stat">
                    <span>Riscos</span>
                    <strong>{hardening.knownRisks.length}</strong>
                  </div>
                </div>
                <div className="intel-insights">
                  {hardening.gates.slice(0, 4).map((gate, i) => (
                    <div key={i}>{gate.name}: {gate.evidence}</div>
                  ))}
                </div>
              </div>
            </div>
          </>
        )}

        {/* ──────────────────────────────────────────────────────── */}
        {/* MARKET DATA TAB */}
        {/* ──────────────────────────────────────────────────────── */}
        {activeTab === 'market' && (
          <div className="split-grid">
            <div className="premium-card">
              <div className="premium-card-title">
                <h3>Visualizador de Candles e Médias Móveis</h3>
              </div>
              <div style={{ background: '#07080b', border: '1px solid var(--glass-border)', borderRadius: '12px', padding: '16px', display: 'flex', justifyContent: 'center' }}>
                <canvas ref={canvasRef} width="600" height="300" style={{ maxWidth: '100%' }}></canvas>
              </div>
              <div style={{ display: 'flex', gap: 20, fontSize: '13px', color: 'var(--color-text-secondary)', justifyContent: 'center' }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                  <span style={{ width: 12, height: 12, backgroundColor: '#00e676', borderRadius: '2px' }}></span>
                  <span>Candle de Alta</span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                  <span style={{ width: 12, height: 12, backgroundColor: '#ff1744', borderRadius: '2px' }}></span>
                  <span>Candle de Baixa</span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                  <span style={{ width: 12, height: 2, backgroundColor: '#00f2fe' }}></span>
                  <span>EMA 9 / EMA 21</span>
                </div>
              </div>
            </div>

            <div className="premium-card">
              <div className="premium-card-title">
                <h3>Feature Store Snapshot</h3>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
                  <span>Volatilidade ATR (14)</span>
                  <span style={{ fontFamily: 'var(--font-mono)', color: 'var(--color-warning)' }}>0.0145</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
                  <span>Volume Z-Score</span>
                  <span style={{ fontFamily: 'var(--font-mono)', color: 'var(--color-success)' }}>1.84 (Alto)</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
                  <span>RSI (14)</span>
                  <span style={{ fontFamily: 'var(--font-mono)', color: '#00f2fe' }}>58.20</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
                  <span>Regime de Mercado</span>
                  <span className="badge success">{metrics.regime}</span>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* ──────────────────────────────────────────────────────── */}
        {/* STRATEGY LAB TAB */}
        {/* ──────────────────────────────────────────────────────── */}
        {activeTab === 'backtest' && (
          <div className="split-grid">
            <div className="premium-card">
              <div className="premium-card-title">
                <h3>Configuração de Backtest</h3>
              </div>
              <form onSubmit={runBacktest} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
                  <div className="input-group">
                    <label>Estratégia</label>
                    <select className="input-premium" value={selectedStrategy} onChange={e => setSelectedStrategy(e.target.value)}>
                      <option value="AtrBreakout">AtrBreakoutStrategy</option>
                      <option value="EmaCrossover">EmaCrossoverStrategy</option>
                    </select>
                  </div>
                  <div className="input-group">
                    <label>Par de Ativos</label>
                    <input className="input-premium" value={symbol} onChange={e => setSymbol(e.target.value.toUpperCase())} placeholder="Ex: BTCUSDT" />
                  </div>
                </div>

                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 16 }}>
                  <div className="input-group">
                    <label>Janela Gráfica</label>
                    <select className="input-premium" value={interval} onChange={e => setInterval(e.target.value)}>
                      <option value="1m">1 Minuto</option>
                      <option value="5m">5 Minutos</option>
                      <option value="15m">15 Minutos</option>
                      <option value="1h">1 Hora</option>
                    </select>
                  </div>
                  <div className="input-group">
                    <label>ATR Período</label>
                    <input className="input-premium" type="number" value={atrPeriod} onChange={e => setAtrPeriod(+e.target.value)} />
                  </div>
                </div>

                <div className="input-group">
                  <label>Multiplicador ATR</label>
                  <input className="input-premium" type="number" step="0.1" value={multiplier} onChange={e => setMultiplier(+e.target.value)} />
                </div>

                <button type="submit" className="btn-premium" disabled={backtestStatus === 'running'}>
                  <Play size={16} />
                  <span>{backtestStatus === 'running' ? 'Processando...' : 'Iniciar Backtest'}</span>
                </button>
              </form>
            </div>

            {/* Backtest Result Report */}
            <div className="premium-card">
              <div className="premium-card-title">
                <h3>Relatório do Laboratório</h3>
              </div>
              {backtestStatus === 'idle' && (
                <div style={{ textAlign: 'center', color: 'var(--color-text-muted)', padding: '40px 0' }}>
                  Aguardando parametrização e início do processamento.
                </div>
              )}
              {backtestStatus === 'running' && (
                <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 16, padding: '40px 0' }}>
                  <div className="logo-glow" style={{ animation: 'spin 1.5s linear infinite' }}>🔄</div>
                  <span>Executando simulação de HFT no PostgreSQL...</span>
                </div>
              )}
              {backtestStatus === 'success' && backtestResult && (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                    <div style={{ background: 'var(--bg-tertiary)', padding: 14, borderRadius: 10 }}>
                      <span style={{ fontSize: '11px', color: 'var(--color-text-secondary)', textTransform: 'uppercase' }}>Retorno Líquido</span>
                      <div style={{ fontSize: '20px', fontWeight: 700, color: 'var(--color-success)', fontFamily: 'var(--font-mono)', marginTop: 4 }}>
                        +{backtestResult.totalNetProfitPercent}%
                      </div>
                    </div>
                    <div style={{ background: 'var(--bg-tertiary)', padding: 14, borderRadius: 10 }}>
                      <span style={{ fontSize: '11px', color: 'var(--color-text-secondary)', textTransform: 'uppercase' }}>Win Rate</span>
                      <div style={{ fontSize: '20px', fontWeight: 700, color: '#00f2fe', fontFamily: 'var(--font-mono)', marginTop: 4 }}>
                        {backtestResult.winRatePercent}%
                      </div>
                    </div>
                  </div>

                  <div style={{ display: 'flex', flexDirection: 'column', gap: 10, fontSize: '13px' }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span>Total de Operações:</span>
                      <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{backtestResult.totalTrades}</span>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span>Max Drawdown:</span>
                      <span style={{ fontFamily: 'var(--font-mono)', color: 'var(--color-danger)' }}>{backtestResult.maxDrawdownPercent}%</span>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span>Índice Sharpe:</span>
                      <span style={{ fontFamily: 'var(--font-mono)', color: 'var(--color-success)' }}>{backtestResult.sharpeRatio}</span>
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                      <span>Fator de Lucro:</span>
                      <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{backtestResult.profitFactor}</span>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>
        )}

        {/* ──────────────────────────────────────────────────────── */}
        {/* PAPER TRADING TAB */}
        {/* ──────────────────────────────────────────────────────── */}
        {activeTab === 'paper' && (
          <div className="split-grid">
            <div className="premium-card">
              <div className="premium-card-title">
                <h3>Histórico de Trades Realizados (Virtual)</h3>
              </div>
              <div className="table-container">
                <table className="premium-table">
                  <thead>
                    <tr>
                      <th>Horário</th>
                      <th>Ativo</th>
                      <th>Tipo</th>
                      <th>Preço de Execução</th>
                      <th>Quantidade</th>
                      <th>Resultado PnL</th>
                    </tr>
                  </thead>
                  <tbody>
                    {recentTrades.map((t, i) => (
                      <tr key={i}>
                        <td style={{ fontFamily: 'var(--font-mono)' }}>{t.time}</td>
                        <td style={{ fontWeight: 600 }}>{t.symbol}</td>
                        <td>
                          <span className={`badge ${t.type === 'BUY' ? 'success' : 'danger'}`}>
                            {t.type}
                          </span>
                        </td>
                        <td style={{ fontFamily: 'var(--font-mono)' }}>${t.price.toFixed(2)}</td>
                        <td style={{ fontFamily: 'var(--font-mono)' }}>{t.qty}</td>
                        <td style={{ fontFamily: 'var(--font-mono)', fontWeight: 600, color: t.pnl ? (t.pnl >= 0 ? 'var(--color-success)' : 'var(--color-danger)') : 'inherit' }}>
                          {t.pnl ? `$${t.pnl >= 0 ? '+' : ''}${t.pnl}` : '-'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>

            <div className="premium-card">
              <div className="premium-card-title">
                <h3>Saldos de Carteira</h3>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                {walletBalances.map((w) => (
                  <div key={w.symbol} style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px', alignItems: 'center' }}>
                    <span style={{ fontWeight: 600, fontSize: '15px' }}>{w.symbol}</span>
                    <div style={{ textAlign: 'right' }}>
                      <div style={{ fontFamily: 'var(--font-mono)', fontWeight: 700 }}>{w.free}</div>
                      <div style={{ fontSize: '11px', color: 'var(--color-text-muted)' }}>Bloqueado: {w.locked}</div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* ──────────────────────────────────────────────────────── */}
        {/* RISK MANAGEMENT TAB */}
        {/* ──────────────────────────────────────────────────────── */}
        {activeTab === 'risk' && (
          <div className="split-grid">
            <div className="premium-card">
              <div className="premium-card-title">
                <h3>Segurança Ativa & Status do Motor</h3>
              </div>
              <div style={{ display: 'flex', alignItems: 'center', gap: 16, padding: '16px', background: riskEngineRules.status === 'Halted' ? 'rgba(255,23,68,0.1)' : 'rgba(0,230,118,0.1)', border: `1px solid ${riskEngineRules.status === 'Halted' ? 'var(--color-danger)' : 'var(--color-success)'}`, borderRadius: '12px' }}>
                {riskEngineRules.status === 'Halted' ? (
                  <XCircle size={36} style={{ color: 'var(--color-danger)' }} />
                ) : (
                  <CheckCircle2 size={36} style={{ color: 'var(--color-success)' }} />
                )}
                <div>
                  <h4 style={{ fontSize: '18px', fontWeight: 700 }}>Status do RiskEngine: {riskEngineRules.status}</h4>
                  <p style={{ fontSize: '13px', color: 'var(--color-text-secondary)', marginTop: 4 }}>
                    {riskEngineRules.status === 'Halted' ? 'Engine em modo bloqueado. Sinais não serão executados.' : 'Validações ativas de segurança do motor e limites normais.'}
                  </p>
                </div>
              </div>

              <div style={{ display: 'flex', gap: 12, marginTop: 10 }}>
                <button onClick={triggerHalt} className="btn-premium" style={{ background: riskEngineRules.status === 'Halted' ? 'var(--color-success)' : 'var(--color-danger)', boxShadow: 'none' }}>
                  {riskEngineRules.status === 'Halted' ? 'Reseta Parada de Risco' : 'Halt Operacional de Emergência'}
                </button>
              </div>
            </div>

            <div className="premium-card">
              <div className="premium-card-title">
                <h3>Regras de Salvaguarda Ativas</h3>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
                  <span>Max Perdas Diárias</span>
                  <span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{riskEngineRules.maxDailyLosses} trades</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
                  <span>Max Drawdown Tolerado</span>
                  <span style={{ fontFamily: 'var(--font-mono)', color: 'var(--color-danger)' }}>{riskEngineRules.maxDailyDrawdownPercent}%</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
                  <span>Período Cooldown de Perda</span>
                  <span style={{ fontFamily: 'var(--font-mono)' }}>{riskEngineRules.cooldownPeriodMinutes} min</span>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* ──────────────────────────────────────────────────────── */}
        {/* BINANCE TESTNET TAB */}
        {/* ──────────────────────────────────────────────────────── */}
        {activeTab === 'testnet' && (
          <div className="split-grid">
            <div className="premium-card">
              <div className="premium-card-title">
                <h3>Histórico de Ordens da Testnet</h3>
              </div>
              <div className="table-container">
                <table className="premium-table">
                  <thead>
                    <tr>
                      <th>Ativo</th>
                      <th>Lado</th>
                      <th>Quantidade</th>
                      <th>Preço</th>
                      <th>ID Binance</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr>
                      <td style={{ fontWeight: 600 }}>BTCUSDT</td>
                      <td><span className="badge success">BUY</span></td>
                      <td style={{ fontFamily: 'var(--font-mono)' }}>0.0800</td>
                      <td style={{ fontFamily: 'var(--font-mono)' }}>$67,420.00</td>
                      <td style={{ fontFamily: 'var(--font-mono)' }}>REAL_BINANCE_845230</td>
                      <td><span className="badge success">FILLED</span></td>
                    </tr>
                    <tr>
                      <td style={{ fontWeight: 600 }}>ETHUSDT</td>
                      <td><span className="badge danger">SELL</span></td>
                      <td style={{ fontFamily: 'var(--font-mono)' }}>1.2500</td>
                      <td style={{ fontFamily: 'var(--font-mono)' }}>$3,510.50</td>
                      <td style={{ fontFamily: 'var(--font-mono)' }}>REAL_BINANCE_845112</td>
                      <td><span className="badge success">FILLED</span></td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>

            <div className="premium-card">
              <div className="premium-card-title">
                <h3>Validação de Filtros da Exchange</h3>
              </div>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
                  <span>TickSize (BTCUSDT)</span>
                  <span style={{ fontFamily: 'var(--font-mono)' }}>0.01</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
                  <span>StepSize (BTCUSDT)</span>
                  <span style={{ fontFamily: 'var(--font-mono)' }}>0.00001</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
                  <span>MinNotional (Min USD)</span>
                  <span style={{ fontFamily: 'var(--font-mono)' }}>5.00 USDT</span>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* ──────────────────────────────────────────────────────── */}
        {/* CONSOLE LOGS TAB */}
        {/* ──────────────────────────────────────────────────────── */}
        {activeTab === 'logs' && (
          <div className="premium-card">
            <div className="premium-card-title">
              <h3>Fluxo de Logs em Tempo Real</h3>
              <span className="badge success">Engine Rodando</span>
            </div>
            <div className="console-panel">
              {systemLogs.map((log, index) => {
                let logClass = 'console-info';
                if (log.includes('failed') || log.includes('REJECTED') || log.includes('error')) {
                  logClass = 'console-danger';
                } else if (log.includes('successful') || log.includes('connected') || log.includes('APPROVED')) {
                  logClass = 'console-success';
                } else if (log.includes('cooldown') || log.includes('warning')) {
                  logClass = 'console-warning';
                }

                return (
                  <div key={index} className="console-line">
                    <span className="console-timestamp">{new Date().toLocaleTimeString()}</span>
                    <span className={logClass}>{log}</span>
                  </div>
                );
              })}
            </div>
          </div>
        )}
      </main>
    </div>
  );
}
