# Fractal ADX Nube
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aproxima el experto MQL original `Fractal_ADX_Cloud` utilizando el indicador Average Directional Index en StockSharp. Funciona con velas de cuatro horas y analiza el cruce de los componentes +DI y -DI. Cuando el componente alcista (+DI) sube por encima del bajista (-DI), la estrategia cierra cualquier posición corta y puede abrir una nueva larga. Si -DI sube por encima de +DI, la lógica se invierte para operaciones cortas.

Las protecciones de stop-loss y take-profit se aplican en unidades de precio absolutas. Los parámetros adicionales permiten habilitar o deshabilitar la apertura y el cierre de posiciones en cada dirección.

## Detalles

- **Criterios de entrada**: Cruce de las líneas +DI y -DI del ADX.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí, usando distancias de precio absolutas.
- **Valores predeterminados**:
  - `AdxPeriod` = 30
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ADX
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: 4h
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
