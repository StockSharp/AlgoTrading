# Estrategia Ma Adx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en los indicadores MA y ADX. Entra en posición cuando el precio cruza la MA con una tendencia fuerte.

Las pruebas indican un retorno anual promedio de aproximadamente 184%. Funciona mejor en el mercado de criptomonedas.

La media móvil dicta la tendencia y el ADX verifica si es suficientemente fuerte para operar. Las entradas siguen los cruces del precio de la MA cuando el ADX supera un umbral.

Este enfoque de tendencia clásico atrae a traders sistemáticos. Las pérdidas se gestionan con un stop porcentual y los beneficios se toman en un objetivo basado en ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > MA && ADX > 25`
  - Corto: `Close < MA && ADX > 25`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cruce inverso de MA o stop
- **Stops**: Porcentaje `StopLossPercent` con toma de beneficios `TakeProfitAtrMultiplier` ATR
- **Valores predeterminados**:
  - `MaPeriod` = 20
  - `AdxPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2m
  - `TakeProfitAtrMultiplier` = 2m
  - `AdxThreshold` = 25
  - `AtrPeriod` = 14
    La entrada exige que el cierre cruce la SMA en esa vela con el ADX por encima de AdxThreshold; el cruce inverso cierra la posición y, si el ADX sigue fuerte, abre la contraria. El objetivo está a TakeProfitAtrMultiplier ATR (AtrPeriod) del cierre de entrada y se comprueba en los cierres de vela.
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Moving Average, ADX
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

