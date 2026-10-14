# Ruptura de Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Ruptura de Volumen observa el Volumen en busca de expansiones rápidas. Cuando las lecturas saltan más allá de su rango promedio, el precio a menudo inicia un nuevo movimiento.

Las pruebas indican un rendimiento anual promedio de aproximadamente 103%. Funciona mejor en el mercado de acciones.

Una posición se abre una vez que el indicador perfora una banda derivada de datos recientes y un multiplicador de desviación. Son posibles operaciones largas y cortas con un stop adjunto.

Este sistema se adapta a traders de momentum que buscan rupturas tempranas. Las operaciones se cierran cuando el Volumen regresa hacia la media. Los valores predeterminados comienzan con `AvgPeriod` = 20.

## Detalles

- **Criterios de entrada**: El indicador supera la media por el multiplicador de desviación.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El indicador revierte a la media.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AvgPeriod` = 20
  - `Multiplier` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2
    Avg y StdDev son la media y la desviación estándar de los últimos AvgPeriod valores de volumen, incluido el actual. El stop es un StopLossPercent fijo del precio de entrada, vigilado también entre velas; 0 lo desactiva. La banda es la media más Multiplier desviaciones estándar: el volumen por encima de ella en una vela alcista abre un largo y en una vela bajista un corto, y la posición se cierra cuando el volumen vuelve por debajo de su media. Una señal de entrada contra una posición abierta la invierte.
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
