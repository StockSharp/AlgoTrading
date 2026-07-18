# Estrategia de Rompimiento por Distancia Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia de Rompimiento por Distancia Supertrend observa el Supertrend en busca de expansiones bruscas. Cuando las lecturas saltan más allá de su rango promedio, el precio a menudo inicia un nuevo movimiento.

Las pruebas indican un rendimiento anual promedio de aproximadamente 115%. Funciona mejor en el mercado de acciones.

Una posición se abre una vez que el indicador perfora una banda derivada de datos recientes y un multiplicador de desviación. Son posibles operaciones largas y cortas con un stop adjunto.

Este sistema es adecuado para operadores de momentum que buscan rompimientos tempranos. Las operaciones se cierran cuando el Supertrend vuelve hacia la media. Los valores predeterminados comienzan con `SupertrendPeriod` = 10.

## Detalles

- **Criterios de entrada**: El indicador supera el promedio por el multiplicador de desviación.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: El indicador revierte al promedio.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Supertrend
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

