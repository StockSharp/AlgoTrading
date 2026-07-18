# Estrategia de Trading Automático ICT NY Kill Zone
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera durante la kill zone de Nueva York utilizando fair value gaps y order blocks.

## Detalles

- **Criterios de entrada**: Fair value gap y order block dentro de la kill zone.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Protección de posición.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `StopLoss` = 30
  - `TakeProfit` = 60
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Price Action
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

