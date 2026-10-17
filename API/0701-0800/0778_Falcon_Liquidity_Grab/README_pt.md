# Estratégia Falcon Liquidity Grab
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera capturas de liquidez durante as principais sessões de mercado usando uma média móvel simples para definir a tendência. Entra quando o preço ultrapassa níveis de swing recentes e reverte com a tendência. Cada operação usa stop loss e take profit fixos medidos em ticks.

## Detalhes

- **Condições de entrada**:
  - **Comprado**: `Low < lowest(swing period)` && `Close > SMA` && `session filter`
  - **Vendido**: `High > highest(swing period)` && `Close < SMA` && `session filter`
- **Condições de saída**: stop loss e take profit fixos.
- **Tipo**: Reversão
- **Indicadores**: SMA, Highest, Lowest
- **Período**: 15 minutos (padrão)
- **Stops**: `StopLossPoints` ticks, `TakeProfitMultiplier`× distância do stop

**Regra exata e padrões:** `SwingPeriod=20`, `TrendLength=50`, `StopLossPoints=50`, `TakeProfitMultiplier=2`, `UseSession=true`, `SessionStart=07:00`, `SessionEnd=21:00` UTC, `CandleType=15 minutos`. A mínima/máxima do candle concluído é comparada com os extremos dos N candles **anteriores**, excluindo o atual. Compra se Low for estritamente menor que a mínima anterior e Close maior que SMA; vende se High exceder a máxima anterior e Close ficar abaixo de SMA. A SMA inclui o candle de sinal. Só entra sem posição na janela UTC (início incluso, fim exclusivo, inclusive janelas noturnas). Proteção nativa com ordens a mercado acompanha bid/ask Level1 entre candles: stop `StopLossPoints × Security.PriceStep`, alvo `TakeProfitMultiplier × distância do stop`. Não existe saída no fechamento por sinal oposto.
