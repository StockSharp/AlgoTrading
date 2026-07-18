# Estrategia de Reversión a la Media de Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este sistema busca volumen de negociación inusualmente alto o bajo en relación con su promedio histórico. Los picos de volumen significativos a menudo revierten a medida que la actividad se normaliza, ofreciendo posibles operaciones en contra del movimiento.

Las pruebas indican un rendimiento anual promedio de aproximadamente 76%. Funciona mejor en el mercado forex.

Se realiza una entrada larga cuando el volumen cae por debajo de la media menos `DeviationMultiplier` veces la desviación estándar y el precio está por debajo de la media móvil. Una entrada corta ocurre cuando el volumen sube por encima de la banda superior con el precio por encima de la media. Las operaciones se cierran una vez que el volumen regresa hacia su nivel medio.

La estrategia beneficia a los traders que observan el agotamiento después de los picos de volumen. Un stop-loss porcentual protege contra escenarios donde el volumen sigue expandiéndose en la misma dirección.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Volume < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Corto**: Volume > Avg + DeviationMultiplier * StdDev && Close > MA
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando volume > Avg
  - **Corto**: Salir cuando volume < Avg
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2m
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Volume
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
