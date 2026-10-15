# Estrategia de Rompimiento de Rango Big Dog
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Big Dog** busca una ventana de consolidación estrecha dentro de la sesión matutina de Londres y opera rompimientos desde esa caja. El asesor experto MQL original colocaba órdenes stop una vez que el rango de precios entre las horas `StartHour` y `StopHour` especificadas permanecía dentro de un número configurable de puntos. El port de StockSharp mantiene la misma idea y usa órdenes de mercado cuando ocurre el rompimiento, acompañadas de niveles dinámicos de stop-loss y take-profit derivados de los extremos de la consolidación.

## Lógica de trading

1. Recopilar velas terminadas entre `StartHour` (inclusive) y `StopHour` (exclusivo por defecto) para construir el rango diario.
2. Ignorar la sesión si la diferencia entre el máximo y mínimo de la sesión supera `MaxRangePoints` (convertido en unidades de precio usando el tamaño de punto ajustado).
3. Después de que la sesión cierre, verificar la distancia entre el mejor ask/bid actual y los niveles de rompimiento. Un setup se activa solo si el mercado está al menos a `DistancePoints` del máximo (para entradas largas) o del mínimo (para entradas cortas).
4. Cuando el precio rompe a través del máximo o mínimo preparado en una vela subsiguiente, entrar con una orden de mercado dimensionada por `OrderVolume` (compensando automáticamente cualquier posición contraria).
5. Asignar inmediatamente las salidas:
   - Las operaciones largas usan un stop-loss en el mínimo de la sesión registrado y un take-profit colocado `TakeProfitPoints` por encima del nivel de entrada.
   - Las operaciones cortas usan un stop-loss en el máximo de la sesión registrado y un take-profit colocado `TakeProfitPoints` por debajo del nivel de entrada.
6. En cada vela terminada la estrategia monitorea el máximo/mínimo para decidir si el stop-loss o take-profit fue alcanzado y cierra la posición en consecuencia.
7. Al comienzo de un nuevo día de trading, todos los niveles en caché se reinician para evitar órdenes sobrantes de la sesión anterior.

> **Puntos ajustados.** La estrategia convierte las entradas basadas en puntos en distancias de precio reales multiplicándolas por el `PriceStep` del instrumento. Cuando el activo tiene 3 o 5 decimales, el valor se escala adicionalmente por 10 para imitar la lógica pip usada en el EA original.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `StartHour` | Hora del día (0-23) cuando comienza la ventana de consolidación. | `14` |
| `StopHour` | Hora del día (0-23) cuando termina la ventana de consolidación. | `16` |
| `MaxRangePoints` | Altura máxima de la caja de sesión medida en puntos ajustados. | `50` |
| `TakeProfitPoints` | Distancia de take-profit en puntos ajustados desde el precio de rompimiento. | `50` |
| `DistancePoints` | Distancia mínima entre el precio actual y el nivel de rompimiento antes de activar órdenes. | `20` |
| `OrderVolume` | Volumen de cada operación de rompimiento (también se aplica al `Volume` de la estrategia). | `1` |
| `CandleType` | Tipo de vela usado para construir la caja de sesión. Marco temporal de una hora por defecto. | `1h` |

## Notas de implementación

- La estrategia se suscribe tanto a velas como al libro de órdenes. Los mejores valores de bid/ask se usan para evaluar los filtros de distancia, recurriendo al último cierre de vela si no hay profundidad disponible.
- Las entradas se ejecutan con órdenes de mercado. Esto refleja el comportamiento de las órdenes stop pendientes originales mientras se mantiene dentro de la API de alto nivel.
- Las decisiones de stop-loss y take-profit se realizan en los cierres de velas basándose en los máximos y mínimos intrabarra, lo que emula los niveles protectores de la versión MQL sin registrar órdenes hijo adicionales.
- La gestión del estado diario cancela cualquier orden activa y reinicia los máximos/mínimos en caché cuando cambia la fecha calendario.
