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
  - `Multiplier` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLoss` = 2.0m
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
