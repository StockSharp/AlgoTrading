# Estrategia Ultimate Balance
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia Ultimate Balance combina ROC, RSI, CCI, Williams %R y ADX en un oscilador ponderado. Una media móvil de este oscilador genera señales: cruzar por encima del nivel de sobreventa activa un largo, mientras que cruzar por debajo del nivel de sobrecompra cierra o revierte la posición.

## Detalles

- **Criterios de entrada**: MA del oscilador cruza por encima de `OversoldLevel`.
- **Largo/Corto**: Ambos (cortos opcionales mediante `EnableShort`).
- **Criterios de salida**: MA del oscilador cruza por debajo de `OverboughtLevel`.
- **Stops**: No.
- **Valores predeterminados**:
  - `WeightRoc` = 2
  - `WeightRsi` = 0.5
  - `WeightCci` = 2
  - `WeightWilliams` = 0.5
  - `WeightAdx` = 0.5
  - `EnableShort` = false
  - `OverboughtLevel` = 0.75
  - `OversoldLevel` = 0.25
  - `MaType` = SMA
  - `MaLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: ROC, RSI, CCI, WilliamsR, ADX
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
