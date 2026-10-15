# Estrategia de Trading por Teoría de Juegos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Trading por Teoría de Juegos combina análisis del comportamiento de manada, detección de trampas de liquidez, flujo institucional y zonas de equilibrio de Nash para operar movimientos contrarios y de momentum.

La estrategia observa los extremos del RSI y los picos de volumen para identificar compras o ventas masivas. Las trampas de liquidez alrededor de máximos y mínimos recientes, junto con el indicador de acumulación/distribución y el sesgo del dinero inteligente, refinan las entradas. Las bandas de precio construidas a partir de una media móvil y la desviación estándar definen el equilibrio de Nash para operaciones de reversión. El tamaño de la posición se adapta cuando el precio está cerca del equilibrio o aparece volumen institucional.

## Detalles
- **Datos**: Velas de precio y volumen.
- **Criterios de entrada**: Señales contrarias, de momentum o de reversión Nash.
- **Criterios de salida**: Stop loss / take profit o señales opuestas.
- **Stops**: Stop loss y take profit opcionales.
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `VolumeMaLength` = 20
  - `HerdThreshold` = 2.0
  - `LiquidityLookback` = 50
  - `InstVolumeMultiplier` = 2.5
  - `InstMaLength` = 21
  - `NashPeriod` = 100
  - `NashDeviation` = 0.02
  - `UseStopLoss` = True
  - `StopLossPercent` = 2
  - `UseTakeProfit` = True
  - `TakeProfitPercent` = 5
- **Filtros**:
  - Categoría: Mixto contrario/momentum
  - Dirección: Largo y Corto
  - Indicadores: RSI, SMA, Accumulation/Distribution, StandardDeviation, Highest/Lowest
  - Complejidad: Avanzado
  - Nivel de riesgo: Medio
