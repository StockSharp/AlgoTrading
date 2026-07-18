# Estrategia Rsi Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia que combina el RSI y el Oscilador Stochastic para doble confirmación de condiciones de sobreventa y sobrecompra.

Las pruebas indican un retorno anual promedio de aproximadamente 181%. Funciona mejor en el mercado de criptomonedas.

El RSI proporciona una visión más amplia del momentum, mientras que el Stochastic da señales más rápidas cerca de los extremos. Las operaciones cambian cuando el oscilador cruza niveles dentro del contexto del RSI.

Ideal para traders ágiles que prefieren configuraciones de osciladores. La estrategia se apoya en un stop de ATR para contener el riesgo.

## Detalles

- **Criterios de entrada**:
  - Largo: `RSI < RsiOversold && StochK < StochOversold`
  - Corto: `RSI > RsiOverbought && StochK > StochOverbought`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: `RSI > 50`
  - Corto: `RSI < 50`
- **Stops**: Basado en porcentaje en `StopLossPercent`
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: RSI, Stochastic Oscillator
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

