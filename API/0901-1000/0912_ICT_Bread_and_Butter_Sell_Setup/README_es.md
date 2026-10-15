# Estrategia ICT Bread and Butter Sell-Setup
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia rastrea los máximos y mínimos de las sesiones de Londres, Nueva York y Asia, y opera configuraciones predefinidas en torno a ellos.

## Detalles

- **Criterios de entrada**:
  - **NY Corto**: el precio alcanza un máximo mayor que el de la sesión de Londres y la vela cierra bajista durante la sesión de NY.
  - **London Close Compra**: entre las 10:30 y las 13:00 si el precio cierra por debajo del mínimo de la sesión de Londres.
  - **Asia Corto**: durante la sesión asiática si el precio cierra por encima del máximo de la sesión asiática.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Cada operación utiliza stop-loss y toma de ganancias definidos en ticks.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `ShortStopTicks` = 10
  - `ShortTakeTicks` = 20
  - `BuyStopTicks` = 10
  - `BuyTakeTicks` = 20
  - `AsiaStopTicks` = 10
  - `AsiaTakeTicks` = 15
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoría: Price action
  - Dirección: Ambos
  - Indicadores: Price action
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
