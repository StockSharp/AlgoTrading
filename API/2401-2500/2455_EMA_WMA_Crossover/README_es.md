# Estrategia de Cruce EMA WMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el cruce entre la media móvil exponencial (EMA) y la media móvil ponderada (WMA) calculadas sobre los precios de apertura de las velas.
Entra en largo cuando la EMA cruza por debajo de la WMA y en corto cuando la EMA cruza por encima de la WMA.
El tamaño de la posición se determina por el porcentaje de riesgo del patrimonio de la cuenta.
La estrategia utiliza distancias fijas de toma de beneficios y stop loss definidas en ticks.

## Detalles

- **Criterios de entrada**:
  - Largo: `EMA crosses below WMA`
  - Corto: `EMA crosses above WMA`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Stop loss o toma de beneficios
- **Stops**: Sí
- **Valores predeterminados**:
  - `EmaPeriod` = 28
  - `WmaPeriod` = 8
  - `StopLossTicks` = 50
  - `TakeProfitTicks` = 50
  - `RiskPercent` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Cruce de medias móviles
  - Dirección: Ambos
  - Indicadores: EMA, WMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
