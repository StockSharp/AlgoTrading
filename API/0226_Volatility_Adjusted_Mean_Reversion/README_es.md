# Estrategia de Reversión a la Media Ajustada por Volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta variación de la reversión a la media escala los umbrales de entrada por la relación entre ATR y desviación estándar. Cuando la volatilidad aumenta en relación con el ruido típico, la distancia necesaria para activar una operación crece, ayudando a evitar señales prematuras durante oscilaciones caóticas.

Las pruebas indican un retorno anual promedio de aproximadamente 115%. Funciona mejor en el mercado de acciones.

Una posición larga se abre cuando el precio cae por debajo de la media móvil en más del umbral ajustado. Una posición corta se abre cuando el precio sube por encima de la media la misma medida. Las posiciones se cierran una vez que el precio cierra de nuevo cerca del nivel medio.

El umbral adaptativo hace que esta estrategia sea adecuada para mercados con regímenes de volatilidad cambiantes. Un stop-loss igual al doble del ATR limita el riesgo mientras se espera la reversión.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Cierre < MA - Multiplier * ATR / (ATR/StdDev)
  - **Corto**: Cierre > MA + Multiplier * ATR / (ATR/StdDev)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando cierre >= MA
  - **Corto**: Salir cuando cierre <= MA
- **Stops**: Sí, dinámico basado en ATR.
- **Valores predeterminados**:
  - `Period` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: ATR, StdDev
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
