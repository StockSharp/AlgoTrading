# Estrategia 3Commas Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Versión simplificada de la estrategia 3Commas Bot. Opera cuando una EMA rápida cruza una EMA más lenta y gestiona el riesgo usando un stop basado en ATR. Se admiten un objetivo de recompensa fijo y un stop trailing de ATR opcional.

## Detalles

- **Criterios de entrada**:
  - **Largo**: la EMA rápida cruza por encima de la EMA lenta.
  - **Corto**: la EMA rápida cruza por debajo de la EMA lenta.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Stop de ATR, take profit opcional, stop trailing de ATR opcional una vez alcanzado el umbral de recompensa.
- **Stops**: Basados en ATR.
- **Valores predeterminados**:
  - `MaLength1` = 21
  - `MaLength2` = 50
  - `AtrLength` = 14
  - `RnR` = 1
  - `RiskM` = 1
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, ATR
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
