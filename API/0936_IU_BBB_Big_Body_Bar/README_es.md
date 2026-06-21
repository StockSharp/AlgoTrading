# Estrategia IU BBB de Barra de Gran Cuerpo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra cuando el cuerpo de la vela actual es varias veces mayor que el tamaño promedio del cuerpo de las últimas 20 velas. Una gran vela alcista abre una posición larga, mientras que una gran vela bajista abre una corta. Las posiciones están protegidas con un stop dinámico basado en ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: cuerpo > cuerpo promedio * BigBodyThreshold y cierre > apertura.
  - **Corto**: cuerpo > cuerpo promedio * BigBodyThreshold y cierre < apertura.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop dinámico ATR.
- **Stops**: Stop dinámico usando ATR * AtrFactor.
- **Valores predeterminados**:
  - `BigBodyThreshold` = 4
  - `AtrLength` = 14
  - `AtrFactor` = 2
  - `CandleType` = 5 minute
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: SMA, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

