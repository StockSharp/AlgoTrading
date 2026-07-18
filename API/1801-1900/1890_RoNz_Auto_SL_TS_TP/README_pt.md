# Estratégia RoNz Auto SL TS TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que abre posições no cruzamento de EMA e gerencia automaticamente os níveis de stop-loss e take-profit.  
Após a entrada, define o stop e o alvo iniciais, depois opcionalmente bloqueia o lucro e ativa um trailing stop.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `EMA10 < EMA20 && EMA10 > EMA100`
  - Vendido: `EMA10 > EMA20 && EMA10 < EMA100`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop loss, take profit, bloqueio de lucro ou trailing stop
- **Stops**: Sim
- **Valores padrão**:
  - `TakeProfit` = 500
  - `StopLoss` = 250
  - `LockProfitAfter` = 100
  - `ProfitLock` = 60
  - `TrailingStop` = 50
  - `TrailingStep` = 10
- **Filtros**:
  - Categoria: Gestão de risco
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: SL/TP/Trailing
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
