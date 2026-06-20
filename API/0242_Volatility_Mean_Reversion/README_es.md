# Estrategia de Reversión a la Media de Volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este enfoque opera en torno a las fluctuaciones de la volatilidad del mercado. Cuando el ATR se desvía notablemente de su media móvil, sugiere que la volatilidad se ha vuelto inusualmente alta o baja y puede revertirse.

Las pruebas indican un rendimiento anual promedio de aproximadamente 73%. Funciona mejor en el mercado de criptomonedas.

La estrategia va largo cuando el ATR cae por debajo de la media menos `DeviationMultiplier` veces la desviación estándar y el precio está por debajo de la media móvil. Va corto cuando el ATR supera la banda superior y el precio está por encima de la media. Las posiciones se cierran una vez que el ATR regresa hacia su nivel medio.

Estas configuraciones funcionan para traders que prefieren operar contra los extremos de volatilidad en lugar de la dirección del precio. Se utiliza un stop-loss protector en caso de que la volatilidad siga expandiéndose.

## Detalles
- **Criterios de entrada**:
  - **Largo**: ATR < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Corto**: ATR > Avg + DeviationMultiplier * StdDev && Close > MA
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando ATR > Avg
  - **Corto**: Salir cuando ATR < Avg
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `AtrPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
