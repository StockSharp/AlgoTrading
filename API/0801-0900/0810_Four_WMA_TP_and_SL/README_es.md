# Estrategia de Cuatro WMA con TP y SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que usa el cruce de cuatro medias móviles con take profit, stop loss y condición de salida alternativa opcionales.

## Detalles

- **Criterios de entrada**:
  - Largo: Long MA1 cruza por encima de Long MA2
  - Corto: Short MA1 cruza por debajo de Short MA2
- **Largo/Corto**: Configurable
- **Stops**: TP y SL basados en porcentaje
- **Valores predeterminados**:
  - `LongMa1Length` = 10
  - `LongMa2Length` = 20
  - `ShortMa1Length` = 30
  - `ShortMa2Length` = 40
  - `MaType` = Wma
  - `EnableTpSl` = true
  - `TakeProfitPercent` = 1m
  - `StopLossPercent` = 1m
  - `Direction` = Both
  - `EnableAltExit` = false
  - `AltExitMaOption` = LongMa1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Medias móviles
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
