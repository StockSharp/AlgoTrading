# Estrategia Sunil BB Blast con Heikin Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina la ruptura de Bandas de Bollinger con la confirmación de velas Heikin Ashi.

La estrategia espera una ruptura de las Bandas de Bollinger alineada con la dirección de la Heikin Ashi anterior y la vela estándar. Las posiciones utilizan la banda opuesta como stop y un objetivo basado en la relación riesgo-recompensa.

## Detalles

- **Criterios de entrada**: El precio rompe las Bandas de Bollinger con la Heikin Ashi anterior y la vela en la misma dirección.
- **Largo/Corto**: Configurable mediante `Direction`.
- **Criterios de salida**: Toma de ganancias o stop-loss basado en las bandas.
- **Stops**: Banda de Bollinger y relación riesgo/recompensa.
- **Valores predeterminados**:
  - `BollingerPeriod` = 19
  - `BollingerMultiplier` = 2m
  - `RiskRewardRatio` = 1m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `Direction` = TradeDirection.Both
  - `SessionBegin` = 09:20:00
  - `SessionEnd` = 15:00:00
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Bollinger, HeikinAshi
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
