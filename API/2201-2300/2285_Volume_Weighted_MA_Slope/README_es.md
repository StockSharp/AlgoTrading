# Estrategia de Pendiente de MA Ponderada por Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Pendiente de MA Ponderada por Volumen** analiza la dirección de la Media Móvil Ponderada por Volumen (VWMA). El sistema entra en una posición larga cuando la VWMA sube durante dos barras consecutivas y abre una posición corta cuando la VWMA baja durante dos barras. Las posiciones existentes se cierran una vez que la pendiente del indicador se revierte.

Este enfoque intenta seguir tendencias emergentes usando promedios de precio ajustados por volumen, filtrando movimientos que ocurren en volumen bajo.

## Detalles

- **Criterios de entrada**: VWMA subiendo durante dos barras (largo) o bajando durante dos barras (corto).
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Pendiente opuesta de la VWMA.
- **Stops**: Sí (configurable, stop loss predeterminado 1% / take profit 2%).
- **Valores predeterminados**:
  - `VwmaPeriod` = 12
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: VWMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Swing
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
