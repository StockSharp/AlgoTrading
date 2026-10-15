# RSI Cíclico Suavizado Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador RSI cíclicamente suavizado. Calcula bandas de percentil dinámicas y opera reversiones cuando el oscilador las cruza.

## Detalles

- **Criterios de entrada**: CRSI cruza por encima de la banda inferior o por debajo de la banda superior.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cruce de la banda opuesta.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `DominantCycleLength` = 20
  - `Vibration` = 10
  - `Leveling` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
