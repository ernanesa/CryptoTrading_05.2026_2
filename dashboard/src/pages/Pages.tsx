import { useRef, useEffect } from 'react';
import { Clock, DollarSign, Layers, AlertTriangle, ShieldAlert, CheckCircle2, Activity, BrainCircuit, XCircle } from 'lucide-react';
import type { MetricsSnapshot } from '../types';

export function OverviewPage({ metrics, hardening, decisionAudits, intelligence, adaptive }: any) {
  const benchmarkStatusClass = (status: string) => {
    if (status.toLowerCase().includes('mandatory')) return 'success';
    if (status.toLowerCase().includes('opt-in')) return 'warning';
    return 'info';
  };

  return (
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
            {hardening.gates.filter((g: any) => g.passed).length}/{hardening.gates.length}
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
                {decisionAudits.slice(0, 5).map((audit: any, i: number) => (
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
            {Object.entries(metrics.strategyScores).map(([strat, score]: [string, any]) => (
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
            <div className="intel-stat"><span>Regime</span><strong>{intelligence.regimeConfidence.toFixed(2)}%</strong></div>
            <div className="intel-stat"><span>Anomalia</span><strong>{intelligence.anomalyScore.toFixed(2)}</strong></div>
            <div className="intel-stat"><span>Volatilidade</span><strong>{intelligence.volatilityForecast.forecastScore.toFixed(2)}</strong></div>
            <div className="intel-stat"><span>Horizonte</span><strong>{intelligence.volatilityForecast.horizonMinutes}m</strong></div>
            <div className="intel-stat"><span>Tendencia</span><strong>{intelligence.featureVector.trendScore.toFixed(2)}</strong></div>
            <div className="intel-stat"><span>Momentum</span><strong>{intelligence.featureVector.momentumScore.toFixed(2)}</strong></div>
            <div className="intel-stat"><span>Meta-label</span><strong>{intelligence.metaLabel.label}</strong></div>
            <div className="intel-stat"><span>Sentimento</span><strong>{intelligence.sentimentRisk.riskBand}</strong></div>
            <div className="intel-stat"><span>Evento</span><strong>{intelligence.eventRisk.severity}</strong></div>
            <div className="intel-stat"><span>Modelos</span><strong>{intelligence.registeredModels.length}</strong></div>
          </div>
          <div className="intel-insights">
            <div>{intelligence.explanation.summary}</div>
            {intelligence.insights.slice(0, 4).map((insight: string, i: number) => (
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
            <div className="intel-stat"><span>Ativa</span><strong>{adaptive.activeStrategyName}</strong></div>
            <div className="intel-stat"><span>Candidata</span><strong>{adaptive.candidateStrategyName}</strong></div>
            <div className="intel-stat"><span>Strategy</span><strong>{adaptive.strategyScore.toFixed(2)}</strong></div>
            <div className="intel-stat"><span>Ativo</span><strong>{adaptive.assetScore.toFixed(2)}</strong></div>
            <div className="intel-stat"><span>Saude</span><strong>{adaptive.marketHealthScore.toFixed(2)}</strong></div>
            <div className="intel-stat"><span>Posicao</span><strong>${adaptive.positionSize.toFixed(2)}</strong></div>
            <div className="intel-stat"><span>Custo</span><strong>{adaptive.executionCost.costBps.toFixed(2)} bps</strong></div>
            <div className="intel-stat"><span>Walk-forward</span><strong>{adaptive.walkForward.verdict}</strong></div>
          </div>
          <div className="intel-insights">
            {adaptive.reasons.slice(0, 4).map((reason: string, i: number) => (
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
            <div className="intel-stat"><span>Gates</span><strong>{hardening.gates.filter((g: any) => g.passed).length}/{hardening.gates.length}</strong></div>
            <div className="intel-stat"><span>Benchmarks</span><strong>{hardening.benchmarks.length}</strong></div>
            <div className="intel-stat"><span>Chaos</span><strong>{hardening.chaosScenarios.filter((c: any) => c.passed).length}/{hardening.chaosScenarios.length}</strong></div>
            <div className="intel-stat"><span>Riscos</span><strong>{hardening.knownRisks.length}</strong></div>
          </div>
          <div className="intel-insights">
            {hardening.gates.slice(0, 4).map((gate: any, i: number) => (
              <div key={i}>{gate.name}: {gate.evidence}</div>
            ))}
          </div>
          <div style={{ display: 'grid', gap: 10, marginTop: 12 }}>
            {hardening.benchmarks.slice(0, 4).map((benchmark: any, i: number) => (
              <div key={i} style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 12, fontSize: 13 }}>
                <span style={{ color: 'var(--color-text-secondary)', overflowWrap: 'anywhere' }}>{benchmark.name}</span>
                <span className={`badge ${benchmarkStatusClass(benchmark.status)}`}>{benchmark.status}</span>
              </div>
            ))}
          </div>
        </div>
      </div>
    </>
  );
}

export function MarketDataPage({ metrics }: { metrics: MetricsSnapshot }) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);

  useEffect(() => {
    if (!canvasRef.current) return;
    const canvas = canvasRef.current;
    const ctx = canvas.getContext('2d');
    if (!ctx) return;

    let animId: number;
    let offset = 0;

    const render = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      
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

        ctx.beginPath();
        ctx.moveTo(x + candleWidth / 2, high);
        ctx.lineTo(x + candleWidth / 2, low);
        ctx.stroke();

        ctx.beginPath();
        ctx.rect(x, Math.min(open, close), candleWidth, Math.abs(close - open));
        ctx.fill();
        ctx.stroke();
      }

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
  }, []);

  return (
    <div className="split-grid">
      <div className="premium-card">
        <div className="premium-card-title">
          <h3>Visualizador de Candles e Médias Móveis</h3>
        </div>
        <div style={{ background: '#07080b', border: '1px solid var(--glass-border)', borderRadius: '12px', padding: '16px', display: 'flex', justifyContent: 'center' }}>
          <canvas ref={canvasRef} width="600" height="300" style={{ maxWidth: '100%' }}></canvas>
        </div>
        <div style={{ display: 'flex', gap: 20, fontSize: '13px', color: 'var(--color-text-secondary)', justifyContent: 'center' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}><span style={{ width: 12, height: 12, backgroundColor: '#00e676', borderRadius: '2px' }}></span><span>Candle de Alta</span></div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}><span style={{ width: 12, height: 12, backgroundColor: '#ff1744', borderRadius: '2px' }}></span><span>Candle de Baixa</span></div>
          <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}><span style={{ width: 12, height: 2, backgroundColor: '#00f2fe' }}></span><span>EMA 9 / EMA 21</span></div>
        </div>
      </div>

      <div className="premium-card">
        <div className="premium-card-title">
          <h3>Feature Store Snapshot</h3>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
            <span>Volatilidade ATR (14)</span><span style={{ fontFamily: 'var(--font-mono)', color: 'var(--color-warning)' }}>0.0145</span>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
            <span>Volume Z-Score</span><span style={{ fontFamily: 'var(--font-mono)', color: 'var(--color-success)' }}>1.84 (Alto)</span>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
            <span>RSI (14)</span><span style={{ fontFamily: 'var(--font-mono)', color: '#00f2fe' }}>58.20</span>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
            <span>Regime de Mercado</span><span className="badge success">{metrics.regime}</span>
          </div>
        </div>
      </div>
    </div>
  );
}

export function PaperTradingPage({ recentTrades, walletBalances }: any) {
  return (
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
              {recentTrades.map((t: any, i: number) => (
                <tr key={i}>
                  <td style={{ fontFamily: 'var(--font-mono)' }}>{t.time}</td>
                  <td style={{ fontWeight: 600 }}>{t.symbol}</td>
                  <td><span className={`badge ${t.type === 'BUY' ? 'success' : 'danger'}`}>{t.type}</span></td>
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
          {walletBalances.map((w: any) => (
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
  );
}

export function RiskManagementPage({ riskEngineRules, triggerHalt }: any) {
  return (
    <div className="split-grid">
      <div className="premium-card">
        <div className="premium-card-title">
          <h3>Segurança Ativa & Status do Motor</h3>
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 16, padding: '16px', background: riskEngineRules.status === 'Halted' ? 'rgba(255,23,68,0.1)' : 'rgba(0,230,118,0.1)', border: `1px solid ${riskEngineRules.status === 'Halted' ? 'var(--color-danger)' : 'var(--color-success)'}`, borderRadius: '12px' }}>
          {riskEngineRules.status === 'Halted' ? <XCircle size={36} style={{ color: 'var(--color-danger)' }} /> : <CheckCircle2 size={36} style={{ color: 'var(--color-success)' }} />}
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
            <span>Max Perdas Diárias</span><span style={{ fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{riskEngineRules.maxDailyLosses} trades</span>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
            <span>Max Drawdown Tolerado</span><span style={{ fontFamily: 'var(--font-mono)', color: 'var(--color-danger)' }}>{riskEngineRules.maxDailyDrawdownPercent}%</span>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
            <span>Período Cooldown de Perda</span><span style={{ fontFamily: 'var(--font-mono)' }}>{riskEngineRules.cooldownPeriodMinutes} min</span>
          </div>
        </div>
      </div>
    </div>
  );
}

export function BinanceTestnetPage() {
  return (
    <div className="split-grid">
      <div className="premium-card">
        <div className="premium-card-title">
          <h3>Histórico de Ordens da Testnet</h3>
        </div>
        <div className="table-container">
          <table className="premium-table">
            <thead>
              <tr><th>Ativo</th><th>Lado</th><th>Quantidade</th><th>Preço</th><th>ID Binance</th><th>Status</th></tr>
            </thead>
            <tbody>
              <tr><td style={{ fontWeight: 600 }}>BTCUSDT</td><td><span className="badge success">BUY</span></td><td style={{ fontFamily: 'var(--font-mono)' }}>0.0800</td><td style={{ fontFamily: 'var(--font-mono)' }}>$67,420.00</td><td style={{ fontFamily: 'var(--font-mono)' }}>REAL_BINANCE_845230</td><td><span className="badge success">FILLED</span></td></tr>
              <tr><td style={{ fontWeight: 600 }}>ETHUSDT</td><td><span className="badge danger">SELL</span></td><td style={{ fontFamily: 'var(--font-mono)' }}>1.2500</td><td style={{ fontFamily: 'var(--font-mono)' }}>$3,510.50</td><td style={{ fontFamily: 'var(--font-mono)' }}>REAL_BINANCE_845112</td><td><span className="badge success">FILLED</span></td></tr>
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
            <span>TickSize (BTCUSDT)</span><span style={{ fontFamily: 'var(--font-mono)' }}>0.01</span>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
            <span>StepSize (BTCUSDT)</span><span style={{ fontFamily: 'var(--font-mono)' }}>0.00001</span>
          </div>
          <div style={{ display: 'flex', justifyContent: 'space-between', borderBottom: '1px solid rgba(255,255,255,0.03)', paddingBottom: '10px' }}>
            <span>MinNotional (Min USD)</span><span style={{ fontFamily: 'var(--font-mono)' }}>5.00 USDT</span>
          </div>
        </div>
      </div>
    </div>
  );
}

export function ConsoleLogsPage({ systemLogs }: { systemLogs: string[] }) {
  return (
    <div className="premium-card">
      <div className="premium-card-title">
        <h3>Fluxo de Logs em Tempo Real</h3>
        <span className="badge success">Engine Rodando</span>
      </div>
      <div className="console-panel">
        {systemLogs.map((log: string, index: number) => {
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
  );
}
