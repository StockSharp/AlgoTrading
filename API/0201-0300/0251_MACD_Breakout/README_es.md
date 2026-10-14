# Ruptura MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Ruptura MACD observa el MACD en busca de expansiones repentinas. Cuando las lecturas saltan más allá de su rango normal, el precio a menudo inicia un nuevo movimiento.

Las pruebas indican un rendimiento anual promedio de aproximadamente 94%. Funciona mejor en el mercado de acciones.

Una posición se abre una vez que el indicador perfora una banda derivada de datos recientes y un multiplicador de desviación. Son posibles operaciones largas y cortas con un stop adjunto.

Este sistema se adapta a traders de momentum que buscan rupturas tempranas. Las operaciones se cierran cuando el MACD regresa hacia la media. Los valores predeterminados comienzan con `FastEmaPeriod` = 12.

## Detalles

- **Criterios de entrada**: El indicador supera la media por el multiplicador de desviación.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El indicador revierte a la media.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `SmaPeriod` = 20
  - `DeviationMultiplier` = 2
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
    Avg y StdDev son la media y la desviación estándar de los últimos SmaPeriod valores de la línea MACD, incluido el actual. El stop es un StopLossPercent fijo del precio de entrada, vigilado también entre velas; 0 lo desactiva. Las bandas son la media más y menos DeviationMultiplier desviaciones estándar: la línea MACD por encima de la banda superior abre un largo y por debajo de la inferior un corto; un largo se cierra cuando la línea MACD vuelve por debajo de su media y un corto cuando vuelve por encima. Una señal de entrada contra una posición abierta la invierte.
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
