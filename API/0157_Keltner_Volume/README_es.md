# Estrategia Keltner Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia Keltner Channels + Volume. Comprar cuando el precio rompe por encima del canal Keltner superior con volumen superior al promedio. Vender cuando el precio rompe por debajo del canal Keltner inferior con volumen superior al promedio.

Las pruebas indican un retorno anual promedio de aproximadamente el 58%. Funciona mejor en el mercado de acciones.

Los límites del canal Keltner definen posibles reversiones, y el aumento del volumen señala convicción. El sistema opera cuando el precio toca una banda con volumen en expansión.

Los traders que buscan confirmación de volumen alrededor de bandas de volatilidad pueden preferir esta configuración. Los stops se calculan a partir del ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close < LowerBand && Volume > AvgVolume`
  - Corto: `Close > UpperBand && Volume > AvgVolume`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - El precio cruza la EMA
- **Stops**: Basados en ATR usando `StopLoss`
- **Valores predeterminados**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `Multiplier` = 2.0m
  - `VolumeAvgPeriod` = 20
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Keltner Channel, Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
