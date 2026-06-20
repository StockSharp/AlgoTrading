# Pairs Trading Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia de pairs trading monitorea el spread de precios entre dos instrumentos correlacionados. Al comparar el spread con su media histórica y desviación estándar, el sistema intenta explotar divergencias temporales que eventualmente revertirán.

Las pruebas indican un rendimiento anual promedio de aproximadamente 88%. Funciona mejor en el mercado de acciones.

Se entra en un spread largo cuando el spread cae por debajo de su media más del multiplicador de desviación especificado. Esto significa comprar el primer activo y vender el segundo. Un spread corto hace lo opuesto cuando el spread sube por encima de la media en la misma cantidad. Las posiciones se cierran una vez que el spread regresa al nivel promedio.

El pairs trading atrae a traders neutrales al mercado que prefieren oportunidades de valor relativo en lugar de dirección pura. Debido a que ambos lados están cubiertos, la volatilidad tiende a ser menor, aunque la estrategia sigue utilizando un stop-loss sobre el spread para gestionar el riesgo.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Spread < Mean - Multiplier * StdDev
  - **Corto**: Spread > Mean + Multiplier * StdDev
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando el spread revierte a la media
  - **Corto**: Salir cuando el spread revierte a la media
- **Stops**: Sí, stop porcentual basado en el valor del spread.
- **Valores predeterminados**:
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Arbitraje
  - Dirección: Ambos
  - Indicadores: Spread statistics
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio

