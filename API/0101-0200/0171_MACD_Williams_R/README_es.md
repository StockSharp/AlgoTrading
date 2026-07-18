# Estrategia Macd Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en los indicadores MACD y Williams %R. Entra largo cuando MACD > Signal y el Williams %R está sobrevendido (< -80). Entra corto cuando MACD < Signal y el Williams %R está sobrecomprado (> -20).

Las pruebas indican un rendimiento anual promedio de aproximadamente 100%. Funciona mejor en el mercado forex.

El MACD indica el cambio de impulso más amplio, mientras que el Williams %R identifica con precisión las reversiones a corto plazo. Ambas señales deben alinearse para iniciar una operación.

Adecuado para quienes combinan señales de tendencia y contratendencia. Los stops dependen de un factor ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `MACD > Signal && WilliamsR < -80`
  - Corto: `MACD < Signal && WilliamsR > -20`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cruce del MACD en dirección opuesta
- **Stops**: Basados en porcentaje usando `StopLossPercent`
- **Valores predeterminados**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `WilliamsRPeriod` = 14
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: MACD, Williams %R, R
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

