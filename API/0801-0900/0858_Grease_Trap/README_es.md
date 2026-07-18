# Estrategia Grease Trap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Grease Trap usa dos medias móviles de longitud Fibonacci y opera en sus cruces con objetivos de ganancia.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: La media rápida cruza por encima de la media lenta.
  - **Corto**: La media rápida cruza por debajo de la media lenta.
- **Criterios de salida**: Objetivo de ganancia o cruce opuesto.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length1` = 9
  - `Length2` = 14
  - `LongProfit` = 0.02
  - `ShortProfit` = 0.02
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: SMA
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
