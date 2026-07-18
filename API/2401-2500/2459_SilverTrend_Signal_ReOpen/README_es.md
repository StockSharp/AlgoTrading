# Estrategia SilverTrend Signal ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador SilverTrend con reapertura opcional. Abre una posición cuando el indicador cambia de dirección y añade posiciones adicionales cada vez que el precio avanza un paso definido a favor de la operación. Las posiciones pueden cerrarse en señales opuestas o cuando se alcanzan los niveles de stop loss / take profit.

## Detalles

- **Criterios de entrada**:
  - Largo: el indicador SilverTrend cambia de tendencia bajista a alcista
  - Corto: el indicador SilverTrend cambia de tendencia alcista a bajista
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Opcionalmente cerrar en señales SilverTrend opuestas
  - Stop Loss o Take Profit alcanzados
- **Stops**: Niveles de precio absolutos
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Ssp` = 9
  - `Risk` = 3
  - `PriceStep` = 300m
  - `PosTotal` = 10
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: SilverTrend
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
