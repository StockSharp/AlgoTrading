# Estratégia Backtest UT Bot + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina um detector de tendência UT Bot com níveis de RSI. Entra comprado em uma reversão altista do UT Bot quando o RSI está sobrevendido e vendido em uma reversão baixista quando o RSI está sobrecomprado.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: UT Bot vira para cima e RSI < `RSI Oversold`.
  - **Vendido**: UT Bot vira para baixo e RSI > `RSI Overbought`.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Percentuais de take profit ou stop loss.
- **Stops**: Take Profit e Stop Loss.
- **Valores padrão**:
  - `RSI Length` = 14
  - `RSI Overbought` = 60
  - `RSI Oversold` = 40
  - `ATR Length` = 10
  - `UT Bot Factor` = 1.0
  - `Take Profit %` = 3.0
  - `Stop Loss %` = 1.5
- **Filtros**:
  - Categoria: Trend Following
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
