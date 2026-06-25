# EMA Cruce de Concurso con Cobertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Recrea la estrategia de MetaTrader "EMA Cross Contest Hedged" usando la API de alto nivel de StockSharp.
- Opera con un par de medias móviles exponenciales (EMA) y opcionalmente confirma con la línea principal del MACD.
- Construye una escalera de órdenes stop pendientes (niveles de "cobertura") después de cada entrada para escalar en tendencias fuertes.
- Aplica niveles estáticos de stop-loss/take-profit expresados en pips y un trailing stop que se activa después de una ganancia mínima.
- Permite elegir si las señales deben usar la vela completada actual o la vela cerrada anterior.

## Indicadores y datos
- EMA corta con longitud configurable (por defecto 4).
- EMA larga con longitud configurable (por defecto 24); el período corto debe permanecer por debajo del período largo.
- MACD (4, 24, 12) línea principal usada como filtro de confirmación opcional.
- Funciona en cualquier marco temporal proporcionado por el parámetro `CandleType` (por defecto velas de 15 minutos).

## Lógica de entrada
1. Esperar una vela terminada del marco temporal configurado.
2. Calcular los valores de EMA rápida y lenta. Dependiendo de `TradeBar`, determinar el cruce usando:
   - La última y la anterior vela terminada (`Current`).
   - La anterior y la que le precede (`Previous`, por defecto).
3. Generar una señal larga cuando la EMA rápida cruza por encima de la EMA lenta. Si `UseMacdFilter` está habilitado, el valor MACD para la misma barra debe ser no negativo.
4. Generar una señal corta cuando la EMA rápida cruza por debajo de la EMA lenta. Con el filtro MACD habilitado, el valor MACD debe ser no positivo.
5. Solo abrir una nueva posición cuando no hay exposición presente (todas las operaciones anteriores están planas).
6. Ejecutar órdenes de mercado con tamaño `OrderVolume`. Después de una entrada, la estrategia:
   - Almacena los niveles de stop-loss y take-profit desplazados por `StopLossPips` y `TakeProfitPips` desde el precio de ejecución.
   - Restablece el estado del trailing stop.
   - Crea cuatro órdenes stop de cobertura espaciadas por `HedgeLevelPips` en la dirección de la operación. Cada orden pendiente hereda la misma distancia de stop-loss/take-profit y expira después de `PendingExpirationSeconds` segundos a menos que el precio la alcance antes.

## Gestión de salida
- **Stop-loss / take-profit:** La estrategia monitorea máximos y mínimos intrabarra. Si el precio toca el stop o el objetivo almacenado, toda la posición se cierra.
- **Trailing stop:** Cuando la ganancia supera `TrailingStopPips + TrailingStepPips`, el stop se sigue a `TrailingStopPips` detrás del último cierre. Las posiciones largas se siguen hacia arriba, las cortas hacia abajo.
- **Cruce opuesto:** Cuando `CloseOppositePositions` está habilitado, la posición se cierra tan pronto como se detecta el cruce EMA opuesto.
- **Escalera pendiente:** Cada orden de cobertura se convierte en una orden de mercado adicional cuando el precio cruza el nivel stop. Las nuevas ejecuciones ajustan el precio de entrada promedio y ajustan los niveles de protección en consecuencia.

## Parámetros
| Nombre | Por defecto | Descripción |
| --- | --- | --- |
| `OrderVolume` | 0.1 | Tamaño de la orden para cada orden de mercado o stop. |
| `StopLossPips` | 140 | Distancia del stop en pips. Poner en 0 para deshabilitar. |
| `TakeProfitPips` | 120 | Distancia del take-profit en pips. Poner en 0 para deshabilitar. |
| `TrailingStopPips` | 30 | Distancia del trailing stop en pips. Poner en 0 para deshabilitar. |
| `TrailingStepPips` | 1 | Ganancia adicional mínima (en pips) antes de que el trailing stop se ajuste de nuevo. |
| `HedgeLevelPips` | 6 | Distancia entre las órdenes stop de cobertura escalonadas. |
| `CloseOppositePositions` | false | Cerrar la posición activa cuando aparezca un cruce opuesto. |
| `UseMacdFilter` | false | Requerir confirmación MACD (>= 0 para largos, <= 0 para cortos). |
| `PendingExpirationSeconds` | 65535 | Vida útil de cada orden stop de cobertura en segundos. |
| `ShortMaPeriod` | 4 | Longitud de la EMA corta. Debe ser menor que `LongMaPeriod`. |
| `LongMaPeriod` | 24 | Longitud de la EMA larga. |
| `TradeBar` | Previous | Determina qué par de barras se usa para detectar el cruce. |
| `CandleType` | 15 minutos | Marco temporal solicitado al proveedor de datos. |

## Notas adicionales
- Los pips se convierten multiplicando `Security.PriceStep` y aplicando automáticamente un factor de 10 para instrumentos de 3 y 5 decimales para coincidir con las convenciones de pip de MetaTrader.
- Las órdenes de cobertura pendientes se simulan dentro de la estrategia y se ejecutan tan pronto como el rango de la vela toca su nivel.
- Se invoca `StartProtection()` para activar los servicios integrados de protección de posición de StockSharp.
- La estrategia mantiene lógica de trailing stop separada para posiciones largas y cortas para reflejar la implementación con cobertura original.
