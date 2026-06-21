# Estrategia Crypto MVRV ZScore
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia aplica el concepto MVRV Z-Score para detectar extremos entre el valor de mercado y el valor realizado.
Las posiciones se abren cuando el z-score del diferencial cruza umbrales predefinidos y se cierran en cruces opuestos.

## Detalles

- **Criterios de entrada**:
  - Largo cuando el z-score del diferencial cruza por encima de `LongEntryThreshold`.
  - Corto cuando el z-score del diferencial cruza por debajo de `ShortEntryThreshold`.
- **Largo/Corto**: Configurable (`TradeDirection`).
- **Criterios de salida**:
  - Cruce del umbral opuesto.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `ZScoreCalculationPeriod` = 252
  - `LongEntryThreshold` = 0.382
  - `ShortEntryThreshold` = -0.382
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: SMA, StandardDeviation, Z-Score
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
