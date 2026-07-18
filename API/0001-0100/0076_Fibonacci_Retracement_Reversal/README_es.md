# Estrategia de Reversión en Retroceso de Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Los mercados frecuentemente retroceden una parte de un movimiento anterior antes de reanudar la tendencia. Esta estrategia identifica los máximos y mínimos recientes del swing y vigila que el precio pruebe los niveles de retroceso del 61.8% o el 78.6%. Estas áreas frecuentemente marcan puntos de agotamiento.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 115%. Funciona mejor en el mercado de acciones.

El algoritmo rastrea los swings en una ventana deslizante y calcula los niveles de Fibonacci entre ellos. Cuando el precio se acerca a un retroceso clave y forma una vela en la dirección de la tendencia original, se abre una operación con un stop colocado a un porcentaje fijo. Los objetivos se sitúan alrededor del punto medio del 50% del swing.

Al centrarse en retrocesos profundos dentro de una tendencia existente, el método busca capturar las etapas iniciales de un movimiento de continuación después de que vendedores o compradores hayan tomado el control brevemente.

## Detalles

- **Criterios de entrada**: El precio prueba el retroceso del 61.8% o el 78.6% e imprime una vela de confirmación.
- **Largo/Corto**: Ambos dependiendo de la tendencia.
- **Criterios de salida**: Precio alcanzando el nivel del 50% o stop-loss.
- **Stops**: Sí, basados en porcentaje.
- **Valores predeterminados**:
  - `SwingLookbackPeriod` = 20
  - `FibLevelBuffer` = 0.5
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Fibonacci levels
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

