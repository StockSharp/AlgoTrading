# Diferencial RSI Dual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Diferencial RSI Dual compara dos períodos de RSI y opera cuando su diferencia cruza un umbral. Este enfoque de doble período busca capturar divergencias entre el momentum a corto y largo plazo.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: `RSI(Long) - RSI(Short)` < `RsiDiffLevel`.
  - **Corto**: `RSI(Long) - RSI(Short)` > `RsiDiffLevel`.
- **Criterios de salida**: Umbral opuesto, período de mantenimiento opcional, take profit/stop loss opcionales.
- **Stops**: Take profit y stop loss opcionales (`Condition`).
- **Valores predeterminados**:
  - `ShortRsiPeriod` = 21
  - `LongRsiPeriod` = 42
  - `RsiDiffLevel` = 5
  - `UseHoldDays` = True
  - `HoldDays` = 5
  - `Condition` = None
  - `TakeProfitPerc` = 15
  - `StopLossPerc` = 10
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo y corto
  - Indicadores: RSI
  - Complejidad: Básico
  - Nivel de riesgo: Medio
