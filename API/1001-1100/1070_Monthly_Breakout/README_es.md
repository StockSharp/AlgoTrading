# Estrategia de Ruptura Mensual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera rupturas del máximo o mínimo del mes actual únicamente durante los meses del calendario seleccionados. La dirección se elige mediante `EntryOption` y las posiciones se cierran tras un número fijo de barras.

## Detalles

- **Criterios de entrada**:
  - Dependen de `EntryOption` y los meses seleccionados (p. ej., largo cuando el cierre cruza por encima del máximo mensual).
- **Largo/Corto**: Configurable.
- **Criterios de salida**: Cierre tras `HoldingPeriod` barras.
- **Stops**: No.
- **Valores predeterminados**:
  - `EntryOption` = LongAtHigh
  - `HoldingPeriod` = 5
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Configurable
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
