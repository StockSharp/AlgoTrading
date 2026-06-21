# Estrategia Mejorada de Bollinger Bands con SL TP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera rebotes en las Bollinger Bands utilizando órdenes límite y stop-loss y take-profit fijos basados en pips.

## Detalles

- **Criterios de entrada**:
  - Largo: cierre anterior <= banda inferior anterior y cierre > banda inferior
  - Corto: cierre anterior >= banda superior anterior y cierre < banda superior
- **Largo/Corto**: Ambos
- **Stops**: Take-profit y stop-loss absolutos en pips
- **Valores predeterminados**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2m
  - `EnableLong` = true
  - `EnableShort` = true
  - `PipValue` = 0.0001m
  - `StopLossPips` = 10m
  - `TakeProfitPips` = 20m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
