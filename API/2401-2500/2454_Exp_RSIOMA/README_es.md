# Estrategia Exp RSIOMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Exp RSIOMA utiliza el indicador RSI de la media móvil (RSIOMA) para operar en reversiones de tendencia y rupturas. Los valores del RSI se suavizan con una media móvil adicional para formar una línea de señal y un histograma. La estrategia soporta cuatro modos:

1. **Breakdown** – opera cuando el RSI cruza los niveles alto/bajo configurados.
2. **HistTwist** – opera cuando el histograma cambia de dirección.
3. **SignalTwist** – opera cuando la línea de señal cambia de dirección.
4. **HistDisposition** – opera cuando el histograma cruza la línea de señal.

Las posiciones pueden abrirse o cerrarse de forma independiente para los lados largo y corto.

## Detalles

- **Criterios de entrada**: depende del `Mode`
- **Largo/Corto**: ambos
- **Criterios de salida**: señal opuesta
- **Stops**: ninguno
- **Valores predeterminados**:
  - `CandleType` = 4 hour
  - `RsiPeriod` = 14
  - `SignalPeriod` = 21
  - `HighLevel` = 20
  - `LowLevel` = -20
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
