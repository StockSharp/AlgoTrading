# Estrategia de Trading Basada en Rachas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Rastrea velas ganadoras y perdedoras consecutivas. Tras alcanzar la racha especificada, la estrategia entra en la dirección opuesta y mantiene la posición durante un número fijo de velas. Las velas Doji se ignoran según el tamaño del cuerpo.

## Detalles

- **Criterios de entrada**: Lado opuesto tras alcanzar la racha de ganancias/pérdidas.
- **Largo/Corto**: Configurable (`TradeDirection`).
- **Criterios de salida**: Después de `HoldDuration` velas.
- **Stops**: No.
- **Valores predeterminados**:
  - `TradeDirection` = Long
  - `StreakThreshold` = 8
  - `HoldDuration` = 7
  - `DojiThreshold` = 0.01
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Configurable
  - Indicadores: Price Action
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
