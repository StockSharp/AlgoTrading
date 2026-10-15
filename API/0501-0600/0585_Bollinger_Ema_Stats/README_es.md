# Estrategia de Bollinger EMA Stats
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza dos conjuntos de Bandas de Bollinger para definir zonas de entrada y stop, y una EMA como objetivo de salida.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Close < banda inferior de Bollinger (multiplicador de entrada).
  - **Corto**: Close > banda superior de Bollinger (multiplicador de entrada).
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Objetivo de beneficio en la EMA.
  - Stop loss en la Banda de Bollinger más amplia.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `BB Length` = 20
  - `Entry StdDev Mult` = 2.0
  - `Stop StdDev Mult` = 3.0
  - `EMA Exit Period` = 20
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, EMA
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Medio plazo
