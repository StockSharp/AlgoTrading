# Estrategia de Reversión a la Media con MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Este método rastrea el histograma MACD en relación con su propio promedio. Las lecturas extremas del histograma a menudo revierten una vez que el impulso cede. Al monitorear la diferencia entre MACD y su línea de señal, la estrategia encuentra movimientos sobreextendidos.

Las pruebas indican un retorno anual promedio de aproximadamente 67%. Funciona mejor en el mercado de acciones.

Se entra en una posición larga cuando el histograma MACD cae por debajo de la media en `DeviationMultiplier` desviaciones estándar. Se abre una posición corta cuando el histograma sube por encima de la media en la misma cantidad. La operación se cierra cuando el histograma vuelve a cruzar su promedio.

Este enfoque es adecuado para traders cómodos operando contra los extremos del impulso. Un stop-loss medido como porcentaje del precio de entrada protege contra las tendencias que continúan fortaleciéndose.

## Detalles
- **Criterios de entrada**:
  - **Largo**: MACD Histogram < Avg - DeviationMultiplier * StdDev
  - **Corto**: MACD Histogram > Avg + DeviationMultiplier * StdDev
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando Histogram > Avg
  - **Corto**: Salir cuando Histogram < Avg
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `FastMacdPeriod` = 12
  - `SlowMacdPeriod` = 26
  - `SignalPeriod` = 9
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mean reversion
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

