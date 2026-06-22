# Estrategia Digital CCI Woodies
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera en el cruce de dos indicadores CCI (Índice de Canal de Materias Primas). Un CCI rápido reacciona rápidamente a los cambios de precio, mientras que un CCI lento suaviza el ruido del mercado. Las señales se generan cuando la línea rápida cruza la lenta.

## Detalles

- **Criterios de entrada**:
  - Largo: el CCI rápido cruza por encima del CCI lento.
  - Corto: el CCI rápido cruza por debajo del CCI lento.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Las posiciones largas se cierran cuando el CCI rápido cruza por debajo del CCI lento.
  - Las posiciones cortas se cierran cuando el CCI rápido cruza por encima del CCI lento.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = velas de 6 horas
  - `FastLength` = 14
  - `SlowLength` = 6
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: CCI
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
