# Estrategia de Posicionamiento Promedio FVG con 200EMA de Auto Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia promedia los niveles de los fair value gaps (FVG) alcistas y bajistas y los combina con una EMA de 200 períodos. Se abre una operación cuando el precio cruza estos promedios en la dirección de la tendencia.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio cruza por encima del promedio de los FVG bajistas y todos los promedios están por encima de la EMA.
  - **Corto**: El precio cruza por debajo del promedio de los FVG alcistas y todos los promedios están por debajo de la EMA.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Stop-loss en el mínimo/máximo reciente.
  - Take profit según la relación riesgo-recompensa.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FvgLookback` = 30
  - `AtrMultiplier` = 0.25
  - `LookbackPeriod` = 20
  - `EmaPeriod` = 200
  - `RiskReward` = 1.5
- **Filtros**:
  - Categoría: Price action
  - Dirección: Ambos
  - Indicadores: ATR, EMA, SMA, Highest, Lowest
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
