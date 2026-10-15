# Estrategia de Cruce MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el cruce del MACD dentro de una zona especificada.

La estrategia de cruce MACD espera a que la línea MACD cruce la línea de señal mientras el valor del MACD se mantiene entre los umbrales inferior y superior. El cruce opuesto cierra la posición existente. No se aplica stop-loss.

## Detalles

- **Criterios de entrada**: Cruce de MACD dentro de la zona.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `LowerThreshold` = -0.5m
  - `UpperThreshold` = 0.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
