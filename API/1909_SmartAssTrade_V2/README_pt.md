# Estratégia SmartAssTrade V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia SmartAssTrade V2 utiliza o histograma MACD e médias móveis de 20 períodos em múltiplos períodos de tempo (1m, 5m, 15m, 30m, 60m) combinados com filtros de Williams %R e RSI para capturar o momentum de tendência. Um trailing stop opcional protege os lucros.

## Detalhes

- **Critérios de entrada**: a maioria dos períodos mostra histograma MACD e MA em ascensão com confirmação de WPR/RSI
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: o preço atinge o take profit ou o stop loss; trailing stop opcional
- **Stops**: Stop loss e take profit absolutos com trailing opcional
- **Valores padrão**:
  - `Volume` = 1
  - `TakeProfit` = 35
  - `StopLoss` = 62
  - `UseTrailingStop` = false
  - `TrailingStop` = 30
  - `TrailingStopStep` = 1
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MACD, SMA, Williams %R, RSI
  - Stops: Opcional
  - Complexidade: Intermediário
  - Período: Multi-período (1m,5m,15m,30m,60m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
