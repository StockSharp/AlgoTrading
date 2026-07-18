# Estrategia Hulk Grid Algorithm V2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de grid que coloca diez órdenes limitadas de compra escalonadas alrededor de un precio medio definido por el usuario. Las órdenes aumentan en tamaño cuanto más cerca están del nivel medio. La estrategia cierra todas las posiciones y cancela las órdenes restantes cuando el precio alcanza un stop-loss por debajo del grid más bajo o un take-profit por encima del grid superior.

## Detalles

- **Criterios de entrada**: Grid de diez órdenes limitadas de compra desde el nivel más bajo hasta el más alto.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Stop-loss por debajo del grid más bajo o take-profit por encima del grid superior.
- **Stops**: Stop-loss y take-profit basados en porcentaje.
- **Valores predeterminados**:
  - `MidPrice` = 0
  - `StopLossPercent` = 2.0
  - `TakeProfitPercent` = 2.0
  - `GridStep` = 200
  - `Lot` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Grid
  - Dirección: Long
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
