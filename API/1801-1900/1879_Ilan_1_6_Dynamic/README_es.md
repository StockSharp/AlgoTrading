# Estrategia de Cuadrícula Dinámica Ilan 1.6
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Ilan 1.6 Dynamic es un asesor experto clásico de cuadrícula y martingala. Abre una operación inicial en una dirección seleccionada y coloca órdenes adicionales cada vez que el precio se mueve en contra de la posición por un paso fijo. El volumen de las nuevas órdenes crece geométricamente por un exponente de lote. Todas las posiciones de la cesta se cierran cuando el precio regresa al precio de entrada promedio más una distancia de take profit. Un stop trailing puede proteger opcionalmente las ganancias si el precio se mueve lo suficiente en la dirección favorable.

El algoritmo se basa únicamente en el movimiento del precio y no utiliza indicadores. Dado que el tamaño de la posición aumenta después de cada movimiento adverso, el sistema conlleva un riesgo elevado pero puede capturar reversiones rápidas.

## Detalles

- **Entrada**
  - La primera orden se abre en la dirección configurada.
  - Se añaden órdenes adicionales cada `PipStep` puntos contra la posición actual, hasta `MaxTrades`.
  - Volumen de cada nueva orden = `InitialVolume * LotExponent^N`.
- **Salida**
  - Cerrar todo cuando el precio toca `AveragePrice ± TakeProfit`.
  - Stop trailing opcional que comienza después de `TrailStart` puntos de ganancia y sigue el precio a distancia `TrailStop`.
- **Gestión de posición**
  - Solo serie larga o solo corta a la vez.
  - Tras cerrar la cesta, la estrategia reinicia desde la dirección inicial.
- **Parámetros**
  - `InitialVolume` – volumen de la primera orden (predeterminado 1).
  - `LotExponent` – multiplicador para el tamaño de órdenes posteriores (predeterminado 1.6).
  - `PipStep` – distancia en puntos entre niveles de cuadrícula (predeterminado 30).
  - `TakeProfit` – objetivo de ganancia desde el precio promedio en puntos (predeterminado 10).
  - `MaxTrades` – número máximo de órdenes activas (predeterminado 10).
  - `StartLong` – abrir la primera operación como largo si es verdadero (predeterminado true).
  - `UseTrailingStop` – activar stop trailing (predeterminado false).
  - `TrailStart` – ganancia en puntos para iniciar el trailing (predeterminado 10).
  - `TrailStop` – distancia de trailing en puntos (predeterminado 10).
  - `CandleType` – marco temporal de velas (predeterminado 1 minuto).
- **Filtros**
  - Categoría: Grid
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Opcional
  - Complejidad: Moderado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
