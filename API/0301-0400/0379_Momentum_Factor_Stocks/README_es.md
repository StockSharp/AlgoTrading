# Estrategia de Acciones por Factor de Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este enfoque sistemático aprovecha el clásico factor de momentum 12-1 meses en renta variable. Al final de cada mes, las acciones se clasifican por su desempeño durante los doce meses anteriores, omitiendo el mes más reciente para evitar reversiones a corto plazo. Los valores en el quintil superior se compran y los del quintil inferior se venden en corto, formando un diferencial neutral al mercado.

El rebalanceo ocurre en el primer día hábil de cada mes. Las posiciones tienen igual ponderación y permanecen abiertas hasta el siguiente rebalanceo; no se utilizan stops explícitos.

Extensa investigación académica e industrial muestra que el momentum ofrece rendimientos excesivos persistentes y brinda una valiosa diversificación cuando se combina con otros factores.

## Detalles

- **Criterios de entrada**: Clasificación mensual por momentum 12-1; largo quintil superior,
  corto quintil inferior
- **Largo/Corto**: Ambos
- **Criterios de salida**: Próximo rebalanceo mensual
- **Stops**: No
- **Valores predeterminados**:
  - `LookbackDays` = 252
  - `SkipDays` = 21
  - `Quintile` = 5
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Cambio de precio
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
