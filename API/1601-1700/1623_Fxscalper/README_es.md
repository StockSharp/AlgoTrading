# Estrategia Fxscalper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de scalping de ruptura de Bandas de Bollinger traducida del experto MQL4 "fxscalper".
La estrategia se suscribe a datos de velas y Bandas de Bollinger. Cuando el precio de cierre rompe por encima de la banda superior abre una posición larga; cuando el precio de cierre rompe por debajo de la banda inferior abre una posición corta. Las posiciones están protegidas por niveles de stop-loss y toma de ganancias.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > Upper Band`
  - Corto: `Close < Lower Band`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta o stops protectores
- **Stops**: Stop loss y toma de ganancias
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2
  - `StopLoss` = 200m
  - `TakeProfit` = 150m
- **Filtros**:
  - Categoría: Bollinger Bands
  - Dirección: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
