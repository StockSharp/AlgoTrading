# Estratégia do Efeito de Janeiro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este exemplo negocia **janelas recorrentes em todos os meses** com filtro de média móvel. **Não** se limita ao efeito clássico de ações de pequenas empresas no fim de dezembro/início de janeiro e não usa dados sobre tamanho de empresa ou vendas tributárias. O retorno anual de 103% anteriormente citado não foi comprovado no replay BTC/TON.

### Regras mensais executadas (C# e Python)

A implementação é uma adaptação **mensal**, não apenas uma operação em dezembro/janeiro. No primeiro candle de 5 minutos concluído e elegível de um novo dia 1–5, compra sem posição se `Close > SMA20`. Nos dias 14–17 encerra long e pode abrir short se estiver sem posição e `Close < SMA20`. Nos dias 26–31 encerra short. As ações exigem mudança de dia e respeitam 50 candles concluídos de pausa. Stop nativo de 2% protege long e short executados independentemente das datas. A rentabilidade acima não foi confirmada no replay BTC/TON.


## Detalhes

- **Critérios de entrada**: ativadores de efeito de calendário
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop-loss ou sinal oposto
- **Stops**: Sim, baseado em percentual
- **Valores padrão**:
  - `CandleType` = 5 minute
  - `StopLoss` = 2%
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Ambos
  - Indicadores: Sazonalidade
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

