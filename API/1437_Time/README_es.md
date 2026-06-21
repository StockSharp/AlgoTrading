# Tiempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que ilustra utilidades de temporización. Compra cuando el máximo supera la apertura por un número de ticks durante una duración especificada.

## Detalles

- **Criterios de entrada**: El máximo menos la apertura permanece por encima del umbral durante los segundos indicados.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: La condición falla.
- **Stops**: No.
- **Valores predeterminados**:
  - `TicksFromOpen` = 0
  - `SecondsCondition` = 20
  - `ResetOnNewBar` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Solo largos
  - Indicadores: Precio
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
