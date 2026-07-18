# Estrategia de Reversión EF Distance
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación en StockSharp del asesor experto MetaTrader "Exp_EF_distance". Reemplaza los indicadores originales EF Distance y Flat-Trend con una media móvil simple (SMA) y un filtro de Average True Range (ATR) para detectar puntos de giro del mercado. El algoritmo observa tres valores consecutivos de SMA e identifica mínimos o máximos locales. Se abre una posición larga cuando la SMA forma un mínimo local y la volatilidad supera el umbral. Se abre una posición corta en el patrón opuesto. Las posiciones se cierran en señales opuestas o cuando se alcanzan los niveles de stop-loss o take-profit.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `SMA(t-1) < SMA(t-2)` y `SMA(t) > SMA(t-1)` y `ATR(t) ≥ AtrThreshold`.
  - **Corto**: `SMA(t-1) > SMA(t-2)` y `SMA(t) < SMA(t-1)` y `ATR(t) ≥ AtrThreshold`.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - Señal inversa en la dirección opuesta.
  - Alcanzado stop-loss o take-profit.
- **Indicadores**:
  - Media Móvil Simple (SMA) – aproximación de EF Distance.
  - Average True Range (ATR) – filtro de volatilidad.
- **Valores predeterminados**:
  - `SMA period` = 10.
  - `ATR period` = 20.
  - `ATR threshold` = 1.
  - `StopLoss` = 100.
  - `TakeProfit` = 200.
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Dos
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Configurable
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí (usa puntos de giro)
  - Nivel de riesgo: Medio
