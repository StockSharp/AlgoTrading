# Estratégia de Reversão BTC Chop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia negocia reversões de curto prazo em BTC quando o preço testa as bandas ATR e o momentum muda, combinando EMA, ATR, RSI, histograma MACD e um filtro de pico de volume.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Low < EMA - ATR*Mult` && `RSI < Oversold` && `MACD hist rising` && `Close > Open` && sem pico de volume vendedor.
  - **Vendido**: `High > EMA + ATR*Mult` && `RSI > Overbought` && `MACD hist falling` && `Close < Open`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - As posições são protegidas por take-profit e stop-loss.
- **Stops**: Take profit 0.75%, Stop loss 0.4%.
- **Valores padrão**:
  - `EMA Period` = 23.
  - `ATR Length` = 55.
  - `ATR Multiplier` = 4.4.
  - `RSI Length` = 9.
  - `RSI Overbought` = 68.
  - `RSI Oversold` = 28.
  - `MACD Fast` = 14.
  - `MACD Slow` = 44.
  - `MACD Signal` = 3.
  - `Volume MA Length` = 16.
  - `Sell Spike Multiplier` = 1.5.
  - `Take Profit (%)` = 0.75.
  - `Stop Loss (%)` = 0.4.
- **Filtros**:
  - Categoria: Reversão.
  - Direção: Ambos.
  - Indicadores: EMA, ATR, RSI, MACD, Volume.
  - Stops: Sim.
  - Complexidade: Médio.
  - Período: Curto prazo.
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Médio.
