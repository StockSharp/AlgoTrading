# Estrategia Silver Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una estrategia de seguimiento de tendencia basada en el indicador SilverTrend personalizado. El indicador construye un canal de precios dinámico utilizando el máximo más alto y el mínimo más bajo en una ventana de lookback y un factor de riesgo. Una señal de trading ocurre cuando el precio cruza el canal y la dirección de la tendencia se revierte.

## Detalles

- **Entrada**: Comprar cuando el indicador cambia a una tendencia alcista. Vender cuando el indicador cambia a una tendencia bajista.
- **Salida**: La posición se revierte en la señal opuesta.
- **Indicadores**: Highest, Lowest, SimpleMovingAverage (dentro del cálculo de SilverTrend).
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Ssp` = 9 — número de barras para el cálculo del canal.
  - `Risk` = 3 — porcentaje que reduce el ancho del canal.
  - `CandleType` = velas de 1 hora.
- **Dirección**: Tanto largo como corto.

El indicador SilverTrend calcula el rango promedio de máximos y mínimos durante `Ssp + 1` barras y encuentra el máximo más alto y el mínimo más bajo durante `Ssp` barras. Los límites del canal son:

```
smin = minLow + (maxHigh - minLow) * (33 - Risk) / 100
smax = maxHigh - (maxHigh - minLow) * (33 - Risk) / 100
```

Si el cierre cae por debajo de `smin`, la tendencia se vuelve bajista. Si el cierre sube por encima de `smax`, la tendencia se vuelve alcista. Se genera una señal cuando la tendencia cambia, y la estrategia invierte inmediatamente su posición en consecuencia.
