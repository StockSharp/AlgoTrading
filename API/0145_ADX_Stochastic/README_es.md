# Adx Stochastic Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia que combina el ADX (Índice Direccional Promedio) para la fuerza de la tendencia y el Oscilador Stochastic para el momento de entrada con condiciones de sobreventa/sobrecompra.

Las pruebas indican un retorno anual promedio de aproximadamente 172%. Funciona mejor en el mercado de divisas.

El ADX resalta la fuerza de la tendencia mientras el Stochastic identifica retrocesos. Las señales largas o cortas aparecen cuando el momentum gira mientras el ADX permanece alto.

Es adecuado para traders que combinan el seguimiento de tendencia con el timing del oscilador. Los stops protectores basados en ATR ayudan a controlar los drawdowns.

## Detalles

- **Criterios de entrada**:
  - Largo: `ADX > AdxThreshold && StochK < StochOversold && Bullish`
  - Corto: `ADX > AdxThreshold && StochK > StochOverbought && Bearish`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Salir cuando `ADX < AdxThreshold`
- **Stops**: Basado en porcentaje en `StopLossPercent`
- **Valores predeterminados**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
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
  - Indicadores: ADX, Stochastic Oscillator
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

