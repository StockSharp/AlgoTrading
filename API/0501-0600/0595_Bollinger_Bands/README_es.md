# Estrategia de Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera rupturas de Bollinger Bands. Compra cuando el precio cierra por encima de la banda superior y vende cuando cierra por debajo de la banda inferior. Sale con un cruce de media móvil simple o cuando se activa el stop loss.

## Detalles

- **Criterios de entrada**:
  - Largo: cierre por encima de la banda superior de Bollinger
  - Corto: cierre por debajo de la banda inferior de Bollinger
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: cierre por debajo de la SMA o precio toca el stop loss
  - Corto: cierre por encima de la SMA o precio toca el stop loss
- **Stops**: Porcentaje del precio de entrada
- **Valores predeterminados**:
  - `BbLength` = 120
  - `BbDeviation` = 2m
  - `SmaLength` = 110
  - `StopLossPercent` = 6m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, SMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
