# Estrategia de Operador por Tiempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el tiempo que entra en largo y/o corto exactamente a una hora de reloj especificada y protege la posición con take profit y stop loss configurables.

## Detalles

- **Criterios de entrada**: A `TradeHour:TradeMinute:TradeSecond` abrir largo si `AllowBuy`, corto si `AllowSell`.
- **Largo/Corto**: Ambos, dependiendo de la configuración
- **Criterios de salida**: posición cerrada mediante stop loss o take profit
- **Stops**: Sí, ambos
- **Valores predeterminados**:
  - `Volume` = 1
  - `TakeProfit` = 0.2
  - `StopLoss` = 0.2
  - `TradeHour` = 0
  - `TradeMinute` = 0
  - `TradeSecond` = 0
  - `AllowBuy` = true
  - `AllowSell` = true
  - `CandleType` = TimeSpan.FromSeconds(1).TimeFrame()
- **Filtros**:
  - Categoría: Tiempo
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

