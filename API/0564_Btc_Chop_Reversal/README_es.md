# Estrategia de Reversión BTC Chop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera reversiones a corto plazo en BTC cuando el precio prueba las bandas ATR y el momentum cambia, combinando EMA, ATR, RSI, histograma MACD y un filtro de pico de volumen.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Low < EMA - ATR*Mult` && `RSI < Oversold` && `MACD hist rising` && `Close > Open` && sin pico de volumen vendedor.
  - **Corto**: `High > EMA + ATR*Mult` && `RSI > Overbought` && `MACD hist falling` && `Close < Open`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Las posiciones están protegidas por toma de ganancias y stop-loss.
- **Stops**: Take profit 0.75%, Stop loss 0.4%.
- **Valores predeterminados**:
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
  - Categoría: Reversión.
  - Dirección: Ambos.
  - Indicadores: EMA, ATR, RSI, MACD, Volumen.
  - Stops: Sí.
  - Complejidad: Medio.
  - Marco temporal: Corto plazo.
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Medio.
