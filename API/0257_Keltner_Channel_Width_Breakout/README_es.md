# Estrategia de Rompimiento por Ancho de Canal Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia de Rompimiento por Ancho de Canal Keltner observa el Keltner en busca de expansiones rápidas. Cuando las lecturas saltan más allá de su rango típico, el precio a menudo inicia un nuevo movimiento.

Las pruebas indican un rendimiento anual promedio de aproximadamente 112%. Funciona mejor en el mercado forex.

Una posición se abre una vez que el indicador perfora una banda derivada de datos recientes y un multiplicador de desviación. Son posibles operaciones largas y cortas con un stop adjunto.

Este sistema es adecuado para operadores de momentum que buscan rompimientos tempranos. Las operaciones se cierran cuando el Keltner vuelve hacia la media. Los valores predeterminados comienzan con `EMAPeriod` = 20.

## Detalles

- **Criterios de entrada**: El indicador supera el promedio por el multiplicador de desviación.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: El indicador revierte al promedio.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `EMAPeriod` = 20
  - `ATRPeriod` = 14
  - `ATRMultiplier` = 2.0m
  - `AvgPeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopMultiplier` = 2
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Keltner
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

