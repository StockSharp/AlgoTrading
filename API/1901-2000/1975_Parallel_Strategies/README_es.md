# Estrategias Paralelas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de ruptura Heikin Ashi con MACD que opera en ambas direcciones. Entra cuando una nueva tendencia Heikin Ashi se alinea con una ruptura por encima o por debajo del Canal Donchian y el MACD confirma el impulso.

Combinar la identificación de tendencia de Heikin Ashi con la detección de rupturas mantiene las operaciones alineadas con movimientos frescos. El MACD actúa como filtro de impulso para evitar señales falsas.

Ideal para traders que buscan entradas tempranas de ruptura tras una reversión de tendencia. Funciona en marcos temporales intradía.

## Detalles

- **Criterios de entrada**:
  - Largo: `Trend turns bullish && Close > DonchianHigh && MACD > Signal`
  - Corto: `Trend turns bearish && Close < DonchianLow && MACD < Signal`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Señal de ruptura opuesta
- **Stops**: No definidos
- **Valores predeterminados**:
  - `DonchianPeriod` = 5
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Heikin Ashi, Donchian Channel, MACD
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
