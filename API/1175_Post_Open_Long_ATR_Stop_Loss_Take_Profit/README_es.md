# Estrategia de Largo Post-Apertura con ATR Stop Loss y Take Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia entra en una posición larga durante la apertura del mercado tras un rompimiento de resistencia mientras el precio permanece cerca de la banda media de Bollinger. Utiliza filtros EMA, RSI, ADX y ATR, y sale mediante stop loss y take profit basados en ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Rompimiento por encima de la resistencia reciente durante la apertura del mercado, precio cerca de la banda media de Bollinger, RSI por encima del umbral, ADX por encima del umbral, tendencia de corto plazo alcista y sin retroceso.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Stop loss o take profit basado en ATR alcanzado.
- **Stops**:
  - Stop loss y take profit basados en ATR.
- **Valores predeterminados**:
  - `BB Length` = 14
  - `BB Mult` = 1.5
  - `EMA Length` = 10
  - `EMA Long Length` = 200
  - `RSI Length` = 7
  - `RSI Threshold` = 30
  - `ADX Length` = 7
  - `ADX Threshold` = 10
  - `ATR Length` = 14
  - `ATR SL Mult` = 2.0
  - `ATR TP Mult` = 4.0
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Long
  - Indicadores: Bollinger Bands, EMA, RSI, ADX, ATR
  - Stops: ATR
  - Complejidad: Intermedio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
