# Estrategia Neon de Ondas de Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Neon de Ondas de Momentum usa cruces del histograma MACD para operar en ambas direcciones. La estrategia va largo cuando el histograma cruza por encima del nivel de entrada (cero por defecto) y va corto cuando cruza por debajo. Las posiciones se cierran cuando el histograma alcanza los niveles de salida configurados.

## Detalles

- **Criterios de entrada**: El histograma MACD cruza el nivel de entrada.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El histograma cruza los niveles de salida de largo/corto.
- **Stops**: No.
- **Valores predeterminados**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 20
  - `EntryLevel` = 0
  - `LongExitLevel` = 11
  - `ShortExitLevel` = -9
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
