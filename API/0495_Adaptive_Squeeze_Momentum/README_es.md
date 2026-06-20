# Estrategia de Momentum Squeeze Adaptativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Momentum Squeeze Adaptativo detecta contracciones de volatilidad cuando las Bandas de Bollinger caen dentro de los Canales de Keltner y espera un rompimiento acompañado de un fuerte momentum. La fuerza del momentum se evalúa mediante un umbral basado en la desviación estándar. Los filtros opcionales de RSI y EMA de tendencia refinan las entradas. El ATR puede usarse para establecer niveles dinámicos de stop-loss y take-profit, y las posiciones se cierran después de un período de mantenimiento basado en el tiempo.

## Detalles

- **Criterios de entrada**:
  - El squeeze se libera (Bandas de Bollinger fuera de los Canales de Keltner).
  - **Largo**: Momentum > umbral dinámico, RSI cruza por encima de sobreventa, EMA de tendencia subiendo (opcional).
  - **Corto**: Momentum < -umbral dinámico, RSI cruza por debajo de sobrecompra, EMA de tendencia bajando (opcional).
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Señal opuesta, stop-loss/take-profit basado en ATR, o salida basada en el tiempo.
- **Stops**: Stop-loss y take-profit opcionales basados en ATR.
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerMultiplier` = 2.0
  - `KeltnerPeriod` = 20
  - `KeltnerMultiplier` = 1.5
  - `MomentumLength` = 12
  - `TrendMaLength` = 50
  - `UseAtrStops` = True
  - `AtrMultiplierSl` = 1.5
  - `AtrMultiplierTp` = 2.5
  - `AtrLength` = 14
  - `MinVolatility` = 0.5
  - `HoldingPeriodMultiplier` = 1.5
  - `UseTrendFilter` = True
  - `UseRsiFilter` = True
  - `RsiLength` = 14
  - `RsiOversold` = 40
  - `RsiOverbought` = 60
  - `MomentumMultiplier` = 1.5
  - `AllowLong` = True
  - `AllowShort` = True
- **Filtros**:
  - Categoría: Ruptura de volatilidad
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, Keltner Channels, Momentum, RSI, EMA, ATR
  - Stops: Opcional
  - Complejidad: Alto
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
