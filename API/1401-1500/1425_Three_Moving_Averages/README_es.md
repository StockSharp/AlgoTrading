# Estrategia de Tres Medias Móviles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera cuando una media móvil corta cruza la media, mientras ambas están alineadas respecto a la media de largo plazo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La MA corta cruza hacia arriba la MA media y la MA media está por encima de la MA larga.
  - **Corto**: La MA corta cruza hacia abajo la MA media y la MA media está por debajo de la MA larga.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `ShortMa` = 20
  - `MediumMa` = 50
  - `LongMa` = 200
