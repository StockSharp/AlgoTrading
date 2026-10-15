# Estrategia I4 DRF v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia I4 DRF v2 aplica el indicador personalizado i4_DRF_v2 que cuenta el número de cierres alcistas y bajistas en una ventana deslizante.
Dependiendo del parámetro TrendModes puede funcionar en modo contrario (Direct) o de seguimiento de tendencia (NotDirect).
La estrategia abre y cierra posiciones cuando el indicador cambia de signo y admite stop loss y take profit opcionales en pasos de precio.

## Detalles

- **Criterios de entrada**: Cambio de signo del indicador según TrendModes
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta o stop loss/take profit
- **Stops**: Sí
- **Valores predeterminados**:
  - `Period` = 11
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `TrendModes` = Direct
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Personalizado
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
