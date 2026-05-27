export interface MetricsSnapshot {
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

export interface IntelligenceSnapshot {
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
  featureVector: any;
  volatilityForecast: any;
  metaLabel: any;
  sentimentRisk: any;
  eventRisk: any;
  ragContext: any;
  explanation: any;
  registeredModels: any[];
  insights: string[];
}

export interface AdaptiveRecommendation {
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
  executionCost: any;
  strategyHealth: any;
  exitPolicy: any;
  walkForward: any;
  banditAllocation: any;
  reasons: string[];
}

export interface HardeningReport {
  version: string;
  isReleaseCandidate: boolean;
  gates: Array<{ name: string; passed: boolean; evidence: string; }>;
  benchmarks: Array<{ name: string; tool: string; status: string; }>;
  chaosScenarios: Array<{ scenario: string; passed: boolean; }>;
  knownRisks: Array<{ area: string; risk: string; mitigation: string; }>;
  alerts: string[];
}

export type RuntimeMode = 'Offline' | 'Simulation' | 'Paper' | 'TestnetDryRun' | 'TestnetReal';

export interface RuntimeStatus {
  mode: RuntimeMode;
  isSimulation: boolean;
  isPaper: boolean;
  isTestnet: boolean;
  isRealTestnet: boolean;
  warnings: string[];
  timestamp: string;
}

export interface TradeLog {
  time: string;
  type: string;
  price: number;
  qty: number;
  pnl?: number;
  symbol: string;
}

export interface AuditLog {
  timestamp: string;
  strategy: string;
  symbol: string;
  signal: string;
  decision: string;
  reason: string;
}
