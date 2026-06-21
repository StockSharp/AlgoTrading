# Reversão de Tendência RSI e ATR com SL TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que utiliza RSI e ATR para detectar reversões de tendência com níveis dinâmicos de stop-loss e take-profit.

## Detalhes

- **Critérios de entrada**: Preço cruzando o limiar adaptativo RSI/ATR.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Integrados através do limiar dinâmico.
- **Valores padrão**:
  - `RsiLength` = 8
  - `RsiMultiplier` = 1.5
  - `Lookback` = 1
  - `MinDifference` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: RSI, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
