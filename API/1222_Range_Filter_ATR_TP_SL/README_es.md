# Estrategia de Filtro de Rango con ATR TP/SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que entra cuando el precio cruza las bandas del filtro de rango y sale usando niveles de take-profit y stop-loss basados en ATR.

## Detalles

- **Criterios de entrada**: El precio cruza por encima de la banda superior para largo, por debajo de la banda inferior para corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Take profit o stop loss basado en ATR.
- **Stops**: Basado en ATR, fijo cuando se abre la operación.
- **Valores predeterminados**:
  - `RangeFilterLength` = 20
  - `RangeFilterMultiplier` = 1.5
  - `AtrLength` = 14
  - `TakeProfitMultiplier` = 1.5
  - `StopLossMultiplier` = 1.5
- **Filtros**: ninguno.
- **Complejidad**: moderado.
- **Marco temporal**: configurable.
