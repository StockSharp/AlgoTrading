# Estrategia de Patrón de 3 Barras MFS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia detecta una secuencia de reversión alcista de tres barras dentro de una tendencia bajista. Busca una gran barra verde de "ignición", un pequeño retroceso rojo y una barra de confirmación alcista que cierra por encima del máximo del retroceso. El filtro de tendencia requiere SMA largo > SMA medio > SMA corto y el cierre de ignición por debajo de la SMA corta.

Una vez que aparece el patrón, la estrategia abre una posición larga, colocando el stop-loss en el mínimo de la barra de ignición y un take-profit en un múltiplo de riesgo-recompensa configurable.

## Detalles

- **Criterios de entrada**: Barra de ignición, retroceso y confirmación en una tendencia bajista.
- **Largo/Corto**: Solo largo.
- **Criterios de salida**: Stop-loss en el mínimo de ignición o take-profit en el múltiplo de riesgo-recompensa.
- **Stops**: Sí, órdenes de stop y objetivo.
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `SmaShortLength` = 20
  - `SmaMedLength` = 50
  - `SmaLongLength` = 200
  - `IgniteMultiplier` = 3
  - `MaxPullbackSize` = 0.33
  - `MinConfirmationSize` = 0.33
  - `RiskReward` = 2
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Largo
  - Indicadores: Candlestick, Moving Average
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
