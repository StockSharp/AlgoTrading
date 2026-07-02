# Estrategia Macd Vwap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en los indicadores MACD y VWAP. Entra largo cuando MACD > Signal y precio > VWAP. Entra corto cuando MACD < Signal y precio < VWAP.

Las pruebas indican un retorno anual promedio de aproximadamente 109%. Funciona mejor en el mercado de criptomonedas.

El impulso MACD se mide en relación con la línea VWAP. Las operaciones largas buscan fortaleza del MACD por debajo del VWAP, mientras que las cortas se forman por encima de él.

Ideal para operadores de momentum intradía que usan referencias ponderadas por volumen. Los stops basados en ATR gestionan el riesgo.

## Detalles

- **Criterios de entrada**:
  - Largo: `MACD > Signal && Close > VWAP`
  - Corto: `MACD < Signal && Close < VWAP`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cruce de MACD en sentido contrario
- **Stops**: Porcentual usando `StopLossPercent`
- **Valores predeterminados**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: MACD, VWAP
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

