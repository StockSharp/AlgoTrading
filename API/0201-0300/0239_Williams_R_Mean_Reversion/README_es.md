# Estrategia de Reversión a la Media con Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Williams %R oscila entre 0 y -100 para mostrar cuándo el precio cierra cerca de los extremos de su rango reciente. Esta estrategia opera contra esos extremos una vez que el indicador se estira lejos de su propio promedio.

Las pruebas indican un retorno anual promedio de aproximadamente 154%. Funciona mejor en el mercado de acciones.

Una operación larga se activa cuando Williams %R cae por debajo del promedio menos `DeviationMultiplier` veces la desviación estándar. Se toma una operación corta cuando sube por encima del promedio más ese multiplicador. Las salidas ocurren cuando Williams %R vuelve hacia su nivel promedio.

El enfoque es adecuado para traders que dependen del agotamiento del impulso para programar las entradas. Un stop-loss de protección limita el riesgo si el precio sigue moviéndose hacia nuevos extremos.

## Detalles
- **Criterios de entrada**:
  - **Largo**: %R < Avg - DeviationMultiplier * StdDev
  - **Corto**: %R > Avg + DeviationMultiplier * StdDev
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando %R > Avg
  - **Corto**: Salir cuando %R < Avg
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `WilliamsRPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mean reversion
  - Dirección: Ambos
  - Indicadores: Williams %R
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

