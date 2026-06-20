# Estrategia de Rebote en Línea de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Los mercados suelen respetar las líneas de tendencia trazadas a través de máximos o mínimos anteriores del swing. Esta estrategia ajusta automáticamente líneas de regresión a la acción reciente del precio y busca velas que reboten desde esas líneas en la dirección de la tendencia dominante.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 124%. Funciona mejor en el mercado de divisas.

Las velas recientes se almacenan para calcular líneas de soporte y resistencia de pendiente ascendente o descendente. Cuando el precio se acerca a una línea de tendencia y una vela confirma el rebote mientras permanece en el lado correcto de una media móvil, el sistema abre una operación. El stop se establece usando un porcentaje del precio y la salida ocurre en un cruce de la media móvil.

Al operar solo en la dirección predominante y esperar una reacción clara en soporte o resistencia, el método intenta capturar movimientos de continuación sin perseguir rupturas.

## Detalles

- **Criterios de entrada**: El precio toca la línea de tendencia calculada y la vela cierra en la dirección de la tendencia por encima/debajo de la MA.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Precio cruzando la media móvil o stop-loss.
- **Stops**: Sí, basados en porcentaje.
- **Valores predeterminados**:
  - `TrendlinePeriod` = 20
  - `MAPeriod` = 20
  - `BounceThresholdPercent` = 0.5
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MA, Trendlines
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

