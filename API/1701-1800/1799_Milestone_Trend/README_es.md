# Estrategia Milestone Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port en StockSharp del asesor experto Milestone 22.5. Opera retrocesos dentro de una tendencia combinando dos medias móviles suavizadas con un filtro de volatilidad y de picos. Cuando una vela rompe el extremo de la barra anterior y la media rápida confirma el movimiento, se abre una posición en la dirección de la tendencia dominante. El ATR evita operar en mercados tranquilos y los cuerpos de velas grandes se tratan como picos.

Las pruebas retrospectivas de la versión MQL original muestran buen rendimiento en los principales pares de divisas. La traducción en C# se enfoca en la claridad y utiliza solo órdenes de mercado para entradas y salidas.

## Detalles

- **Criterios de entrada**:
  - Fuerza de tendencia entre `MinTrend` y `MaxTrend`.
  - La vela rompe el máximo o mínimo anterior y la SMA rápida confirma.
  - ATR por encima de `MinRange` y cuerpo de vela por debajo de `CandleSpike`.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: La señal opuesta cierra la posición.
- **Stops**: No implementados; la señal opuesta actúa como stop.
- **Valores predeterminados**:
  - `SlowMaPeriod` = 120
  - `FastMaPeriod` = 30
  - `AtrPeriod` = 14
  - `MinTrend` = 10
  - `MaxTrend` = 100
  - `MinRange` = 5
  - `CandleSpike` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA, ATR
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
