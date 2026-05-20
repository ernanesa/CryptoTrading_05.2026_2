# 22 — Frontend Version Strategy

Data-base: **2026-05-20 UTC-03 / America/Maceio**.

## Decisão

Usar versões estáveis no caminho principal do projeto.

## Baseline aprovada

| Tecnologia | Versão alvo | Decisão |
|---|---|---|
| Vite | 8.x estável | Usar no projeto principal |
| TypeScript | 6.0.x estável | Usar no projeto principal |
| React | 19.2.x ou superior dentro da linha 19, com patches de segurança | Usar no projeto principal |
| React 20 | Não usar até existir release estável oficial | Avaliar futuramente |
| TypeScript 7 | Não usar até existir release estável oficial | Avaliar futuramente |

## Justificativa

Vite 8 já aparece como versão atual na documentação oficial e deve ser usado como baseline do frontend.

TypeScript 7 e React 20 não devem ser adotados no caminho principal enquanto não estiverem estáveis e documentados oficialmente. O projeto pode ter uma trilha experimental separada para testar versões prerelease/canary/nightly, mas essas versões não entram no MVP sem ADR, testes e validação de compatibilidade.

## Regras

- Não usar dependência prerelease no caminho principal sem ADR.
- Fixar versões no `package.json` e no lockfile.
- Atualizar dependências apenas com changelog revisado.
- Rodar build, testes, lint e dashboard smoke test após upgrades.
- Manter branch separada para experimentos de versões não estáveis.
- Se uma versão prerelease trouxer ganho relevante, provar com benchmark antes de promover.

## Estratégia de experimento

Criar uma branch separada quando necessário:

```text
experiment/frontend-next
```

Nessa branch podem ser testados:

- React canary;
- TypeScript nightly/prerelease;
- Vite prerelease;
- plugins alternativos;
- compiladores/build tools experimentais.

Critérios para promover uma versão experimental:

- [ ] release estável publicada;
- [ ] documentação oficial disponível;
- [ ] compatibilidade com Vite/plugin React confirmada;
- [ ] build passa;
- [ ] testes passam;
- [ ] bundle size não piora sem justificativa;
- [ ] HMR/dev server estável;
- [ ] dashboard funcional;
- [ ] ADR criado.

## Baseline prática inicial

```json
{
  "devDependencies": {
    "vite": "^8.0.10",
    "typescript": "^6.0.3",
    "@vitejs/plugin-react": "latest-compatible"
  },
  "dependencies": {
    "react": "^19.2.1",
    "react-dom": "^19.2.1"
  }
}
```

Observação: no momento da implementação, verificar novamente npm, changelogs oficiais e eventuais alertas de segurança antes de fixar a versão final.
