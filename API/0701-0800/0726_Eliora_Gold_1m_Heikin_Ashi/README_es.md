# Estrategia Eliora Gold 1m Heikin Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia utiliza velas Heikin Ashi en un marco temporal de un minuto. Entra en posición con velas fuertes alineadas con la tendencia cuando el mercado no está en consolidación, y aplica un período de enfriamiento entre operaciones. Las salidas se gestionan mediante un stop trailing basado en ATR.

## Detalles

- **Criterios de entrada**: vela Heikin Ashi fuerte en dirección de la tendencia, sin consolidación, filtro de volatilidad.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: stop trailing basado en ATR.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AtrPeriod` = 14
  - `CooldownBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Heikin Ashi, ATR, SMA, Highest/Lowest
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
