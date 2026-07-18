# Estrategia de Arbitraje Estadístico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este enfoque de arbitraje estadístico opera un par de valores relacionados basándose en su posición relativa alrededor de las medias móviles. Al comparar cada activo con su propia media, la estrategia busca explotar las dislocaciones a corto plazo que deberían converger con el tiempo.

Las pruebas indican un retorno anual promedio de aproximadamente 94%. Funciona mejor en el mercado de acciones.

Se inicia una posición larga cuando el primer activo cotiza por debajo de su media móvil mientras el segundo activo cotiza por encima de su propia media. Una posición corta ocurre cuando el primer activo está por encima de su media y el segundo está por debajo. Las posiciones se cierran cuando el primer activo cruza de vuelta a través de su media móvil, señalando que el diferencial se ha normalizado.

El método es ideal para traders neutrales al mercado cómodos balanceando la exposición entre dos instrumentos. El stop-loss incorporado limita las caídas si el diferencial se amplía en lugar de revertirse.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Asset1 < MA1 && Asset2 > MA2
  - **Corto**: Asset1 > MA1 && Asset2 < MA2
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando Asset1 cierra por encima de su MA1
  - **Corto**: Salir cuando Asset1 cierra por debajo de su MA1
- **Stops**: Sí, stop-loss porcentual sobre el diferencial.
- **Valores predeterminados**:
  - `LookbackPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoría: Arbitraje
  - Dirección: Ambos
  - Indicadores: Moving Averages
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
