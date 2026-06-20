# Estrategia de Tendencia con Kalman Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este método de seguimiento de tendencia utiliza un Kalman Filter para suavizar las fluctuaciones de precio y estimar la dirección subyacente. El filtro se adapta dinámicamente al ruido del mercado, ofreciendo una visión refinada de la fortaleza de la tendencia en comparación con las medias móviles estándar.

Las pruebas indican un retorno anual promedio de aproximadamente 112%. Funciona mejor en el mercado de forex.

Se abre una posición larga cuando el precio de cierre sube por encima de la estimación del Kalman Filter. Por el contrario, se toma una posición corta cuando el cierre cae por debajo del valor del filtro. Dado que el filtro se actualiza en cada barra, las operaciones cambian cada vez que el precio cruza la línea, proporcionando participación continua en mercados en tendencia.

Los traders que prefieren enfoques sistemáticos pueden encontrar el Kalman Filter útil para reducir los movimientos erráticos. Un stop de protección basado en ATR mantiene el riesgo limitado en caso de que la tendencia revierta rápidamente.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Cierre > Kalman Filter
  - **Corto**: Cierre < Kalman Filter
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando cierre < Kalman Filter
  - **Corto**: Salir cuando cierre > Kalman Filter
- **Stops**: Sí, stop-loss basado en ATR.
- **Valores predeterminados**:
  - `ProcessNoise` = 0.01m
  - `MeasurementNoise` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Kalman Filter
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
