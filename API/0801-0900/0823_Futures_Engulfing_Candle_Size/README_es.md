# Estrategia de Futuros con Patrón de Vela Envolvente por Tamaño
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera una vez al día cuando el rango de una vela supera un umbral de ticks dentro de una ventana horaria seleccionada. La dirección sigue el cuerpo de la vela y la salida se realiza mediante take profit y stop loss.

## Detalles

- **Criterios de entrada**: Rango de la vela en ticks dentro de la sesión de trading.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Take profit o stop loss.
- **Stops**: Take Profit y Stop Loss.
- **Valores predeterminados**:
  - `CandleType` = 1 minute
  - `CandleSizeThresholdTicks` = 25
  - `TakeProfitTicks` = 50
  - `StopLossTicks` = 40
  - `StartHour` = 7
  - `StartMinute` = 0
  - `EndHour` = 9
  - `EndMinute` = 15
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Candlestick
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
