# Ichimoku Daily Candle X Hull MA X MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina las líneas adelantadas de Ichimoku, la dirección de la vela diaria, la tendencia de la Hull Moving Average y un MACD basado en HMA. Las posiciones largas se abren cuando todos los componentes se alinean alcistas; las cortas ocurren cuando todas las condiciones giran bajistas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: HMA en ascenso, precio actual por encima del HMA anterior, vela diaria actual mayor que la anterior, SenkouA > SenkouB, línea MACD > señal.
  - **Corto**: HMA en descenso, precio por debajo del HMA anterior, vela diaria actual menor que la anterior, SenkouA < SenkouB, línea MACD < señal.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `HmaPeriod` = 14
  - `ConversionPeriod` = 9
  - `BasePeriod` = 26
  - `SpanPeriod` = 52
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `PriceSource` = Open
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Ichimoku, Hull MA, MACD
  - Stops: Ninguno
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
