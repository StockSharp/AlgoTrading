# Estrategia Ticker Pulse Meter + Fear EKG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina períodos cortos y largos para detectar condiciones de sobreventa y recuperaciones.
Compra cuando el percentil combinado cruza el disparador superior y sale en un cruce de toma de beneficios.

## Detalles

- **Criterios de entrada**: el percentil cruza por encima de `EntryThresholdHigh` o por debajo de `OrangeEntryThreshold`
- **Largo/Corto**: Solo largos
- **Criterios de salida**: cruce por debajo de `ProfitTake`
- **Stops**: No
- **Valores predeterminados**:
  - `LookbackShort` = 50
  - `LookbackLong` = 200
  - `ProfitTake` = 95
  - `EntryThresholdHigh` = 20
  - `EntryThresholdLow` = 40
  - `OrangeEntryThreshold` = 95
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Largo
  - Indicadores: Highest, Lowest
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
