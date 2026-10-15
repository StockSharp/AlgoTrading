# Estrategia X2MA JJRSX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina un filtro de tendencia de doble media móvil con un disparador de entrada basado en RSI.
La tendencia se define en un marco temporal superior comparando una media rápida y una lenta.
Las entradas se ejecutan en un marco temporal inferior cuando el RSI sale de las zonas de sobreventa o sobrecompra en la dirección de la tendencia.

## Detalles

- **Criterios de entrada**:
  - Largo: tendencia alcista y RSI cruza por encima de `Oversold`
  - Corto: tendencia bajista y RSI cruza por debajo de `Overbought`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Umbral RSI opuesto o reversión de tendencia
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `TrendCandleType` = velas de 4h
  - `SignalCandleType` = velas de 30m
  - `FastMaPeriod` = 12
  - `SlowMaPeriod` = 5
  - `RsiPeriod` = 8
  - `Overbought` = 70
  - `Oversold` = 30
