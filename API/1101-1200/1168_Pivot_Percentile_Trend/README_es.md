# Estrategia Pivot Percentile Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula la fuerza de tendencia basada en percentiles en múltiples períodos de retroceso y la combina con un filtro SuperTrend. Las operaciones largas ocurren cuando la fuerza de la tendencia es positiva y el precio está por encima de la línea SuperTrend. Las operaciones cortas se toman cuando la fuerza de la tendencia es negativa y el precio está por debajo del SuperTrend.

## Detalles

- **Indicadores**: análisis de percentiles, SuperTrend
- **Dirección**: Ambos
- **Marco temporal**: Cualquiera

## Cálculo de este ejemplo

Siete ventanas `PercentileLength * 1..7` usan cuartiles High/Low (75%/25%, rango más cercano). Cada ventana aporta +1 si el High actual supera estrictamente el cuartil superior y -1 si el Low queda estrictamente por debajo del inferior. La vela actual se incluye en cada ventana.

La suma determina la fuerza de tendencia. Fuerza positiva y Close por encima del SuperTrend de precio abren largo; fuerza negativa y Close por debajo abren corto. Una posición contraria se invierte completamente hasta `Volume`. Una fuerza neutra o un filtro rechazado mantienen la posición. Todas las ventanas deben estar completas; no hay pausa entre señales ni stop protector.

Valores iniciales: `PercentileLength=40`, `AtrPeriod=10`, `AtrMultiplier=3`, `CandleType=5m`. SuperTrend usa ATR de Wilder y las bandas de precio de la plataforma.
