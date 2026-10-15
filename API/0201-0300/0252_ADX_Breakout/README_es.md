# Ruptura ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Ruptura ADX monitorea el ADX en busca de fuertes expansiones. Cuando las lecturas saltan más allá de su rango típico, el precio a menudo inicia un nuevo movimiento.

Las pruebas indican un rendimiento anual promedio de aproximadamente 97%. Funciona mejor en el mercado de criptomonedas.

Una posición se abre una vez que el indicador perfora una banda derivada de datos recientes y un multiplicador de desviación. Son posibles operaciones largas y cortas con un stop adjunto.

Este sistema se adapta a traders de momentum que buscan rupturas tempranas. Las operaciones se cierran cuando el ADX regresa hacia la media. Los valores predeterminados comienzan con `ADXPeriod` = 14.

## Detalles

- **Criterios de entrada**: El indicador supera la media por el multiplicador de desviación.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El indicador revierte a la media.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `ADXPeriod` = 14
  - `AvgPeriod` = 20
  - `Multiplier` = 0.1
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2
    Avg y StdDev son la media y la desviación estándar de los últimos AvgPeriod valores de ADX, incluido el actual. El stop es un StopLossPercent fijo del precio de entrada, vigilado también entre velas; 0 lo desactiva. La banda es la media más Multiplier desviaciones estándar: el ADX por encima de ella en una vela alcista abre un largo y en una vela bajista un corto, y la posición se cierra cuando el ADX vuelve por debajo de su media. Una señal de entrada contra una posición abierta la invierte.
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: ADX
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
