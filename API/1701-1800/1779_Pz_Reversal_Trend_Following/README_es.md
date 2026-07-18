# PZ Reversión con Seguimiento de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia sigue las rupturas de máximos y mínimos a largo plazo. Compra cuando el precio de cierre supera el máximo más alto del período de retrospección y vende en corto cuando el precio de cierre cae por debajo del mínimo más bajo. La posición siempre se revierte ante señales opuestas, manteniendo la estrategia continuamente en el mercado.

El enfoque intenta capturar tendencias sostenidas entrando después de una ruptura significativa. Dado que el sistema opera solo en extremos importantes, puede evitar el ruido menor, pero puede incurrir en grandes caídas durante condiciones laterales.

## Detalles

- **Criterios de entrada**: Ruptura del máximo/mínimo de las `Period` barras anteriores.
- **Largo/Corto**: Ambas direcciones, siempre en el mercado.
- **Criterios de salida**: Señal de ruptura opuesta.
- **Stops**: No
- **Valores predeterminados**:
  - `Period` = 100
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Highest, Lowest
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
