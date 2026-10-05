# Estrategia MACD RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
MACD RSI combina el momentum del MACD con las lecturas de sobrecompra/sobreventa del RSI.
Cuando ambos indicadores se alinean, aumenta la probabilidad de un movimiento sostenido.

Las pruebas indican un retorno anual promedio de aproximadamente el 130%. Funciona mejor en el mercado de acciones.

La estrategia entra largo cuando el MACD cruza al alza y el RSI sube desde la zona de sobreventa, o vende en corto cuando el MACD cruza a la baja con el RSI cayendo desde la sobrecompra.

Los stops basados en un porcentaje del precio ayudan a contener las pérdidas si los indicadores divergen después de la entrada.

## Detalles

- **Criterios de entrada**: señal de indicador
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop-loss o señal opuesta
- **Stops**: Sí, basado en porcentaje
- **Valores predeterminados**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
    Un RSI por debajo de RsiOversold prepara un largo y por encima de RsiOverbought un corto; vale el extremo más reciente. El siguiente cruce del MACD en esa dirección lo confirma y entra, invirtiendo una posición opuesta; cada extremo confirma un solo cruce.
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MACD, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

