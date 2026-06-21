# Estrategia Pivot Point SuperTrend con Filtro de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina una línea SuperTrend basada en pivotes con un filtro de tendencia SuperTrend y una confirmación de media móvil. Opera cuando la tendencia cambia o cuando aparece una señal de Pivot SuperTrend dentro de una ventana de fechas.

## Detalles

- **Criterios de entrada**:
  - El filtro de tendencia cambia hacia arriba y el precio está por encima de la media móvil.
  - Pivot SuperTrend emite una señal de compra dentro del rango de fechas configurado.
- **Criterios de salida**:
  - El filtro de tendencia cambia hacia abajo o Pivot SuperTrend emite una señal de venta.
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `PivotPeriod` = 2
  - `Factor` = 3
  - `AtrPeriod` = 10
  - `TrendAtrPeriod` = 10
  - `TrendMultiplier` = 3
  - `MaPeriod` = 20
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Pivot, SuperTrend, SMA
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: Opcional
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
