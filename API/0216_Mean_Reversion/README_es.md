# Mean Reversion Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Este enfoque estadístico busca extremos a corto plazo en el precio en relación con su promedio reciente. La estrategia utiliza una media móvil para definir el valor justo y mide la desviación de esa media mediante un cálculo de desviación estándar.

Las pruebas indican un rendimiento anual promedio de aproximadamente 85%. Funciona mejor en el mercado de criptomonedas.

Las operaciones se abren cuando el precio empuja a una distancia establecida del promedio. Una caída por debajo de la banda inferior activa una entrada larga, anticipando un rebote hacia la media, mientras que un rally por encima de la banda superior provoca un corto. Una vez que el precio toca la media móvil de nuevo, se cierra cualquier posición abierta.

El método atrae a traders con estilo contrario que desean zonas de entrada y salida claramente definidas. Debido a que se basa en bandas basadas en volatilidad, se adapta a mercados más tranquilos o más activos manteniendo las pérdidas bajo control mediante un stop-loss fijo.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Price < MA - k*StdDev (below lower band)
  - **Corto**: Price > MA + k*StdDev (above upper band)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando el precio cruza por encima de la media móvil
  - **Corto**: Salir cuando el precio cruza por debajo de la media móvil
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MovingAveragePeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Mean Reversion
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

