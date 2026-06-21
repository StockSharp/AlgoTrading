# Estrategia RSI Solo Largos con Retornos Confirmados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia espera a que el RSI caiga por debajo de un umbral y luego vuelva a cruzar por encima. El retorno confirma condiciones de sobreventa antes de entrar en una posición larga. Las posiciones se cierran cuando el RSI cruza por encima de un nivel de salida. Los parámetros permiten operaciones cortas, pero los valores predeterminados las desactivan en la práctica.

## Detalles

- **Criterios de entrada**: El RSI cruza por encima del nivel de sobreventa tras haber estado por debajo.
- **Largo/Corto**: Solo largos por defecto.
- **Criterios de salida**: El RSI cruza por encima del nivel de salida largo o se activan las reglas cortas opcionales.
- **Stops**: No.
- **Valores predeterminados**:
  - `CandleType` = 5 minute
  - `RsiLength` = 14
  - `Oversold` = 44
  - `LongExitLevel` = 70
  - `ShortEntryLevel` = 100
  - `ShortExitLevel` = 0
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Largo
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
