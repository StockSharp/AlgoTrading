# Estrategia de Cruce de EMA TQQQ Más Poderosa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia entra en largo cuando la EMA rápida cruza por encima de la EMA lenta. El take profit y el stop loss se establecen como multiplicadores del precio de entrada.

## Detalles

- **Criterios de entrada**: EMA rápida cruzando por encima de la EMA lenta
- **Largo/Corto**: Solo largos
- **Criterios de salida**: Precio alcanzando el nivel de take profit o stop loss
- **Stops**: Sí (multiplicador fijo)
- **Valores predeterminados**:
  - `FastLength` = 20
  - `SlowLength` = 50
  - `TakeProfitMultiplier` = 1.3
  - `StopLossMultiplier` = 0.95
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: EMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
