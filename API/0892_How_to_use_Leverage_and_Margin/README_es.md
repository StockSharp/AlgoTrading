# Cómo Usar Apalancamiento y Margen — Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un sistema de cruce de oscilador Stochastic. La estrategia compra cuando la línea %K cruza por encima de %D por debajo del nivel 80 y vende en corto cuando %K cruza por debajo de %D por encima del nivel 20. Las posiciones están protegidas por un take‑profit medido en ticks.

## Detalles

- **Criterios de entrada**:
  - **Largo**: %K cruza por encima de %D y %K < 80.
  - **Corto**: %K cruza por debajo de %D y %K > 20.
- **Largo/Corto**: Ambos
- **Criterios de salida**: Take‑profit o cruce opuesto
- **Stops**: Sí, take‑profit en ticks
- **Valores predeterminados**:
  - `Stochastic Period` = 13
  - `%K Period` = 4
  - `%D Period` = 3
  - `Take Profit Ticks` = 100
  - `CandleType` = 1 minuto
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Stochastic
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
