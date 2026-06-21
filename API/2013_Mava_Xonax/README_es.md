# Estrategia MAVA Xonax
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia usa medias móviles exponenciales de precios de apertura y cierre para detectar cambios de dirección. Las distancias de stop loss y take profit se derivan de las EMAs del máximo y mínimo, asegurando que las operaciones tengan niveles de riesgo y recompensa predefinidos.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La EMA de apertura cruza por encima de la EMA de cierre usando las dos últimas barras completadas.
  - **Corto**: La EMA de apertura cruza por debajo de la EMA de cierre usando las dos últimas barras completadas.
- **Largo/Corto**: Ambos
- **Stops**: Stop loss y take profit fijos basados en rangos de EMA.
- **Valores predeterminados**:
  - `EmaPeriod` = 6
  - `CandleType` = TimeSpan.FromMinutes(240).TimeFrame()
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Largo plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
