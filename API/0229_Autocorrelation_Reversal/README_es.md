# Estrategia de Reversión por Autocorrelación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia analiza la autocorrelación de precios a corto plazo para evaluar si los movimientos recientes tienen probabilidad de revertirse. La autocorrelación negativa sugiere que los cambios de precio sucesivos tienden a alternar de dirección, creando condiciones de reversión a la media.

Las pruebas indican un retorno anual promedio de aproximadamente 124%. Funciona mejor en el mercado de divisas.

Cuando la autocorrelación calculada cae por debajo del umbral y el precio está por debajo de una media móvil, el sistema compra anticipando un rebote. Si la autocorrelación es negativa y el precio está por encima del promedio, se abre una posición corta. Las salidas ocurren cuando el precio cruza el promedio o la autocorrelación sube por encima del umbral.

El enfoque es adecuado para traders que buscan ventajas estadísticas en lugar de patrones de gráfico. Se aplica un stop-loss porcentual para protegerse contra tendencias sostenidas que violen la reversión esperada.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Autocorrelation < Threshold && Close < MA
  - **Corto**: Autocorrelation < Threshold && Close > MA
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando Close > MA o autocorrelation > Threshold
  - **Corto**: Salir cuando Close < MA o autocorrelation > Threshold
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `AutoCorrPeriod` = 20
  - `AutoCorrThreshold` = -0.3m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mean reversion
  - Dirección: Ambos
  - Indicadores: Autocorrelation, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

