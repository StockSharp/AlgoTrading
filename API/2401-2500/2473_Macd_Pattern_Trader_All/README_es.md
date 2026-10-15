# Estrategia de Patrón Trader MACD (All)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que abre posiciones en reversiones bruscas del MACD. Busca dos grandes picos alrededor de un pequeño valor intermedio de la línea MACD. Se abre una venta cuando el valor anterior del MACD es positivo y el valor actual cae profundamente en territorio negativo. Se abre una compra en la condición opuesta. El stop loss y el take profit se derivan de los máximos y mínimos recientes.

El algoritmo se adapta a mercados volátiles donde el momentum cambia de dirección rápidamente. Utiliza únicamente órdenes de mercado y calcula los niveles de riesgo a partir del historial de velas.

## Detalles

- **Criterios de entrada**: Relación de picos MACD basada en `RatioThreshold`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop en el extremo reciente más el offset o pico opuesto.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastEmaPeriod` = 24
  - `SlowEmaPeriod` = 13
  - `StopLossBars` = 22
  - `TakeProfitBars` = 32
  - `OffsetPoints` = 40
  - `RatioThreshold` = 5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
