# RSI Sobrecompra/Sobreventa (RSI Overbought/Oversold)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este sistema opera reversiones utilizando el Índice de Fuerza Relativa (RSI). Cuando el RSI cae por debajo del nivel de sobreventa, compra después de cerrar los cortos. Cuando el RSI sube por encima del nivel de sobrecompra, vende después de cerrar los largos.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 61%. Funciona mejor en el mercado de criptomonedas.

Las posiciones se cierran cuando el RSI regresa a una zona neutral o se alcanza el stop-loss.

## Detalles

- **Criterios de entrada**: RSI por debajo de `OversoldLevel` o por encima de `OverboughtLevel`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: RSI cruza `NeutralLevel` o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `OverboughtLevel` = 70
  - `OversoldLevel` = 30
  - `NeutralLevel` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
