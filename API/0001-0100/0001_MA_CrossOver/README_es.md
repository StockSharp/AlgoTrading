# Estrategia de cruce de medias móviles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia sigue la relación entre una media móvil exponencial rápida y otra lenta. Un cruce alcista abre o revierte a una posición larga, mientras que un cruce bajista abre o revierte a una posición corta. Las señales se evalúan únicamente con velas finalizadas.

## Detalles

- **Entrada larga**: la EMA rápida cruza por encima de la EMA lenta.
- **Entrada corta**: la EMA rápida cruza por debajo de la EMA lenta.
- **Salida**: un cruce opuesto revierte la posición; un stop-loss porcentual puede cerrarla antes.
- **Valores predeterminados**:
  - `FastLength` = 100
  - `SlowLength` = 400
  - `StopLossPercent` = 2
  - `CandleType` = 1 minuto
- **Implementaciones**: C# y Python.
