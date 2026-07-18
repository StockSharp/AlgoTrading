# Modelo de Oscilador de Spread de Arbitraje de Volatilidad (VASOM)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Toma una posición larga en el futuro VIX del mes frontal cuando el RSI del spread entre los contratos del primer y segundo mes cae por debajo de un umbral. La posición se cierra cuando el RSI sube por encima de un nivel de salida.

## Detalles
- **Criterios de entrada**: RSI del spread < `LongThreshold`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: RSI del spread > `ExitThreshold`.
- **Stops**: No.
- **Valores predeterminados**:
  - `RsiPeriod` = 2
  - `LongThreshold` = 46
  - `ExitThreshold` = 76
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `SecondSecurity` = "CBOE:VX2!"
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Solo largos
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
