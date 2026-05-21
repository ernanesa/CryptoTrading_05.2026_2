# 00 — Operating Rules

Data-base: **2026-05-21 UTC-03 / America/Maceio**.

## 1. Repositório oficial de escrita

A partir deste planejamento, o único repositório que deve receber escrita/alteração é:

```text
ernanesa/CryptoTrading_05.2026_2
```

Repositórios anteriores são apenas referência/leitura:

- `ernanesa/CryptoTrading_v5.0`;
- `ernanesa/CryptoTrading_05.2026`;
- `ernanesa/Bettina`.

## 2. Verificar data atual

Antes de planejar ou desenvolver:

- [ ] verificar data atual;
- [ ] registrar data-base;
- [ ] pesquisar versões atuais se houver tecnologia, API ou biblioteca envolvida.

## 3. Nunca chutar

Se não souber, pesquisar. Se houver dúvida, pesquisar. Se houver histórico técnico, consultar o RAG local.

Ordem recomendada:

1. Documentação oficial.
2. Código do repositório atual.
3. RAG local.
4. ADRs e `plans/`.
5. Repositórios anteriores em modo read-only.
6. Releases/issues oficiais das bibliotecas.

## 4. Planejar antes de codar

Toda atividade precisa ter:

- objetivo;
- entrega de valor;
- critérios de aceite;
- checklist;
- riscos;
- testes esperados;
- documentação a atualizar.

## 5. Atualizar checklists

Ao final de cada atividade:

- [ ] atualizar checklist local;
- [ ] atualizar `plans/19-master-checklists.md` se aplicável;
- [ ] atualizar README/docs se mudou comportamento;
- [ ] criar/atualizar ADR se houve decisão arquitetural.

## 6. Arquitetura sem atalhos

- Estratégia não fala direto com executor.
- ML não executa ação diretamente.
- Sentimento não executa ação diretamente.
- Decisões relevantes passam pelo RiskEngine.
- Tudo relevante gera DecisionAudit.

## 7. Preferir libs maduras

Antes de criar código próprio, verificar biblioteca madura para:

- Binance;
- indicadores;
- estatística;
- persistência;
- resiliência;
- observabilidade;
- ML;
- testes;
- dashboard.

## 8. Fluxo de Git e Desenvolvimento (Branch & Commit)

Ao iniciar qualquer atividade ou desenvolvimento de card:

1. **Criar Branch Específica**: Nunca desenvolva diretamente na branch `main`. Crie uma branch com nome semântico para o card/atividade atual.
2. **Desenvolver Focado**: Desenvolva tudo referente ao card/atividade de forma isolada nesta branch.
3. **Validar e Testar**: Ao finalizar, teste tudo localmente e tenha absoluta certeza de que tudo está funcionando perfeitamente como esperado.
4. **Atualizar Andamento**: Atualize o andamento da atividade nos checklists locais e no `plans/19-master-checklists.md`.
5. **Commit Semântico**: Faça commits utilizando mensagens semânticas padronizadas (ex: `feat:`, `fix:`, `docs:`, `chore:`).
6. **Checkout e Atualização da Main**: Faça checkout para `main`, atualize a `main` integrando as alterações desenvolvidas.
7. **Reiniciar o Fluxo**: Crie uma nova branch para a próxima atividade e faça o processo todo novamente.

