# Estratégia de Reversão de Tendência EMA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que entra comprada no cruzamento de EMA com confirmação do RSI e sai quando ocorre o cruzamento oposto com RSI abaixo do nível. Usa take profit e stop loss baseados em percentual.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `FastEMA crosses above SlowEMA && RSI > RsiLevel`
- **Comprado/Vendido**: Somente comprado
- **Stops**: Take profit e stop loss percentuais
- **Valores padrão**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `RsiLength` = 14
  - `RsiLevel` = 50m
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Tendência
  - Direção: Somente comprado
  - Indicadores: EMA, RSI
  - Stops: Sim
  - Complexidade: Iniciante
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
