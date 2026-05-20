# 16 — Quality, Security, DoR and DoD

## Definition of Ready

Uma atividade só começa quando:

- [ ] data atual verificada;
- [ ] escopo claro;
- [ ] RAG consultado quando necessário;
- [ ] fontes oficiais consultadas se houver API/lib externa;
- [ ] bibliotecas avaliadas antes de código próprio;
- [ ] critérios de aceite definidos;
- [ ] testes definidos;
- [ ] riscos listados.

## Definition of Done

Uma atividade só termina quando:

- [ ] build passa;
- [ ] testes relevantes passam;
- [ ] checklist atualizado;
- [ ] documentação atualizada;
- [ ] nenhum secret real versionado;
- [ ] logs não vazam secrets;
- [ ] eventos relevantes auditáveis;
- [ ] riscos remanescentes registrados.

## Segurança

- chaves fora do repositório;
- redaction de logs;
- configuração via env/secret manager;
- permissões mínimas;
- auditoria de decisões;
- circuit breakers;
- halt manual e automático.
