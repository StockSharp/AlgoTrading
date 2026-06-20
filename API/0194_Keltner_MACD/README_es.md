# Keltner Macd Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en los Canales Keltner y MACD. Entra largo cuando el precio rompe por encima del canal Keltner superior con MACD > Signal. Entra corto cuando el precio rompe por debajo del canal Keltner inferior con MACD < Signal. Sale cuando MACD cruza su línea de señal en la dirección opuesta.

Las pruebas indican un retorno anual promedio de aproximadamente 169%. Funciona mejor en el mercado de criptomonedas.

Las rupturas del Canal Keltner sirven como disparador y el momentum del MACD filtra la dirección. La estrategia inicia operaciones cuando ambas señales se alinean.

Ideal para traders que persiguen expansiones de volatilidad con respaldo de momentum. Un stop basado en ATR contiene el riesgo.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > UpperBand && MACD > Signal`
  - Corto: `Close < LowerBand && MACD < Signal`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cruce de MACD en sentido contrario
- **Stops**: Basado en ATR usando `AtrMultiplier`
- **Valores predeterminados**:
  - `EmaPeriod` = 20
  - `Multiplier` = 2m
  - `AtrPeriod` = 14
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Keltner Channel, MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

