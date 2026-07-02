# Estrategia Yeong RRG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en la fuerza relativa normalizada y la razón de momentum (RRG).

La estrategia entra en largo cuando tanto JDK RS como JDK RoC están por encima de 100 y sale cuando ambos caen por debajo de 100.

## Detalles

- **Criterios de entrada**: JDK RS y JDK RoC por encima de 100.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: JDK RS y JDK RoC por debajo de 100.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Relative Strength
  - Dirección: Long
  - Indicadores: SMA, ROC, StandardDeviation
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

