# Distribuidores comerciales v7.51 RIVOT (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen

Dealers Trade v7.51 es una estrategia de cuadrícula estilo martingala que se entregó originalmente como el MetaTrader 4 asesor experto `Dealers_Trade_v_7.51_RIVOT.mq4`. El puerto mantiene la idea original de operar lejos de un sesgo direccional basado en pivotes, escalando hacia el lado dominante cada vez que el precio retrocede una distancia de pip configurable. La implementación StockSharp utiliza ayudas estratégicas de alto nivel para suscribirse a velas, calcular las zonas de pivote y gestionar el tamaño, el riesgo y las salidas de las posiciones.

## Lógica de trading

1. **Marco pivote**
   - La estrategia construye dos precios de referencia para cada vela terminada:
     - **Pivote clásico** (`P`) = `(previous high + previous low + previous close + current open) / 4`.
     - **Pivote flotante** (`FLP`) = `(current high + current low + current close) / 3`.
   - Una brecha en pips entre `P` y `FLP` debe ser mayor o igual a `GapThreshold` para permitir el comercio de la barra actual.

2. **Sesgo direccional**
   - Cuando el cierre de la vela está por encima de ambos pivotes y el filtro de brecha se satisface, el sesgo cambia a **largo**.
   - Cuando el cierre de la vela está por debajo de ambos pivotes con la brecha confirmada, el sesgo cambia a **corto**.
   - El sesgo permanece vigente hasta que la serie de posiciones está completamente cerrada o aparece la condición opuesta después de que finaliza la serie.

3. **Entradas de escala**
   - Sólo puede haber una serie de operaciones activas a la vez.
   - La primera entrada sigue inmediatamente el sesgo.
   - Las entradas adicionales se abren solo cuando el precio retrocede contra el sesgo activo en al menos `PipDistance` pips desde el llenado más reciente, emulando el promedio de martingala original.
   - Cada nuevo pedido multiplica el tamaño anterior por `VolumeMultiplier` pero nunca excede `MaxVolume`.
   - El número de entradas apiladas está limitado por `MaxTrades`.

4. **Controles de riesgo**
   - Un stop-loss estricto a `StopLoss` pips desde la entrada promedio ponderada por volumen cierra toda la serie.
   - Una toma de ganancias fija en `TakeProfit` pips bloquea las ganancias una vez que el precio vuelve a ser favorable.
   - Cuando está habilitado, el trailing-stop bloquea dinámicamente las ganancias acercándose al precio cada vez que se mueve más de `TrailingStop` pips más allá de la entrada promedio.

5. **Restablecer condiciones**
   - Cualquier salida completa (stop-loss, take-profit, trailing-stop o aplanamiento manual de posición) restablece los contadores martingala y elimina el sesgo direccional.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `Volume` | 1 | Tamaño de pedido base para la primera entrada. |
| `MaxTrades` | 5 | Número máximo de entradas promediadas por serie. |
| `PipDistance` | 4 | Movimiento adverso mínimo (en pips) requerido antes de agregar una nueva posición. |
| `TakeProfit` | 15 | Distancia desde la entrada promedio ponderada por volumen para cerrar toda la red con ganancias. |
| `StopLoss` | 90 | Distancia desde la entrada media que desencadena una salida protectora. |
| `TrailingStop` | 15 | La compensación de trailing-stop se aplica una vez que el precio se mueve a favor; establezca en cero para desactivar el seguimiento. |
| `VolumeMultiplier` | 1.5 | Factor utilizado para aumentar el tamaño del pedido para cada entrada posterior. |
| `MaxVolume` | 5 | Límite para el volumen de un solo pedido después de aplicar el multiplicador. |
| `GapThreshold` | 7 | Espacio mínimo (en pips) entre los pivotes clásico y flotante requerido para activar el sesgo. |
| `CandleType` | Velas con marco de tiempo de 15 minutos | Tipo de vela utilizada para cálculos y toma de decisiones. |

Todos los parámetros se configuran a través de `StrategyParam<T>` para que puedan optimizarse dentro de StockSharp Designer o Strategy Runner.

## Notas de uso

- La estrategia se basa únicamente en datos de velas; no se requiere ningún flujo directo de oferta/demanda a nivel de tick. Asegúrese de que su proveedor de datos pueda entregar el `CandleType` seleccionado.
- Debido a que StockSharp agrega posiciones de forma predeterminada, la implementación mantiene un promedio interno ponderado por volumen para emular el libro de cuadrícula MT4. Si se producen llenados parciales, la contabilidad de posiciones incorporada mantiene los valores consistentes.
- La representación del gráfico agrega dos líneas horizontales (`Pivot` y `FloatingPivot`) al área del gráfico cuando está disponible.
- No existe comercio inverso automático; el sistema espera a que termine la serie en curso antes de aceptar un cambio de sesgo.

## Diferencias con la versión MQL

- El guión original dibujó múltiples etiquetas y comentarios en el gráfico MT4. El puerto mantiene solo la lógica comercial funcional y reemplaza las imágenes con StockSharp líneas del gráfico.
- Las funciones de protección de cuentas basadas en el total de órdenes abiertas, el filtrado manual de números mágicos y las tablas de valores de pips de símbolos específicos no son obligatorias en StockSharp y se omitieron.
- El cierre de la orden a precios de tick exactos (`Ask == tp`) en el código MetaTrader se aproxima con comparaciones de precios en los cierres de velas.
- La gestión comercial se implementa con órdenes de mercado (`BuyMarket`/`SellMarket`) en lugar de bucles de tickets MT4. Las paradas y salidas dinámicas ocurren en las actualizaciones de velas.

## Mejores prácticas

- Pruebe siempre la estrategia en operaciones en papel o simulaciones históricas con modelos realistas de diferenciales/comisiones antes de ponerlas en funcionamiento.
- Considere reducir `VolumeMultiplier` o `MaxTrades` en instrumentos altamente volátiles para controlar la reducción.
- Para productos intradiarios, ajuste `CandleType` para que coincida con la granularidad de datos de la configuración original (el valor predeterminado es 15 minutos, pero EA se usó con frecuencia en M15 y H1).

## Archivos

- `CS/DealersTradeV751RivotStrategy.cs` – Implementación principal de C#.
- `README_zh.md` – Documentación china.
- `README_ru.md` – Documentación rusa.
