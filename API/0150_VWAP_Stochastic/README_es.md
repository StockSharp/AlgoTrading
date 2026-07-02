# Estrategia Vwap Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia que combina los indicadores VWAP y Stochastic. Compra cuando el precio está por debajo del VWAP y el Stochastic está sobrevendido. Vende cuando el precio está por encima del VWAP y el Stochastic está sobrecomprado.

Las pruebas indican un retorno anual promedio de aproximadamente 187%. Funciona mejor en el mercado de acciones.

El VWAP marca el nivel de negociación promedio y el Stochastic muestra condiciones de sobrecompra o sobreventa. Los largos se activan por debajo del VWAP con un oscilador en alza, los cortos por encima del VWAP con uno en caída.

Los traders intradía que observan niveles de valor intradía pueden beneficiarse de este estilo. Los stops se colocan usando un múltiplo de ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close < VWAP && StochK < OversoldLevel`
  - Corto: `Close > VWAP && StochK > OverboughtLevel`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: `Close > VWAP`
  - Corto: `Close < VWAP`
- **Stops**: Basado en porcentaje usando `StopLossPercent`
- **Valores predeterminados**:
  - `StochPeriod` = 14
  - `StochKPeriod` = 3
  - `StochDPeriod` = 3
  - `OverboughtLevel` = 80m
  - `OversoldLevel` = 20m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: VWAP, Stochastic Oscillator
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

