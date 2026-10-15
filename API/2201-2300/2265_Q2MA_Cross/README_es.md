# Estrategia de Cruce Q2MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Cruce Q2MA opera basándose en el cruce de medias móviles suavizadas construidas sobre los precios de cierre y apertura de las velas. Se abre una posición larga cuando la media del cierre cae por debajo de la media de la apertura tras haber estado por encima, mientras que se abre una posición corta en el cruce opuesto. Las posiciones se cierran cuando aparece una tendencia contraria. La estrategia también aplica niveles de stop loss y take profit medidos en ticks.

## Detalles

- **Criterios de entrada**: cruce entre medias móviles de precios de cierre y apertura
- **Largo/Corto**: ambas direcciones
- **Criterios de salida**: cruce opuesto o stop loss/take profit
- **Stops**: sí
- **Valores predeterminados**:
  - `Length` = 8
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Volume` = 1
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `Invert` = false
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Moving Average
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: H4
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
