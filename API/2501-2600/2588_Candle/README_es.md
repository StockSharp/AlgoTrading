# Estrategia de Vela (Candle)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Vela (Candle)** es un puerto directo del clásico experto MT5 "Candle.mq5". Evalúa el color de cada vela completada en el marco temporal seleccionado y mantiene la posición alineada con el cierre más reciente. Las velas alcistas llevan la estrategia al largo, las bajistas al corto, y las planas dejan la posición sin cambios. El riesgo se controla mediante distancias de take-profit y trailing stop en pips que se convierten a precios absolutos a través del tamaño de tick del instrumento.

La estrategia solo reacciona después de que una vela se ha formado completamente para evitar el ruido dentro de la barra. Una retrospectiva obligatoria (`MinBars * 2` velas completadas) valida que el gráfico contiene suficiente historial, mientras que un período de enfriamiento configurable espera entre operaciones. Esto produce una implementación fiel en StockSharp de la lógica MetaTrader original sin depender del acceso a series de bajo nivel.

## Lógica de trading
### Preparación
- Procesa velas proporcionadas por `CandleType`; no se requieren otras fuentes de datos.
- Espera hasta que se hayan procesado al menos `2 * MinBars` velas completadas antes de permitir entradas.
- Opera solo cuando la estrategia está en línea, formada y tiene permitido ejecutar órdenes.
- Aplica el intervalo `TradeCooldown` (por defecto 10 segundos) entre cualquier dos operaciones.

### Reglas de entrada y reversión
1. **Estado plano:**
   - Entrar largo (`BuyMarket`) cuando una vela cierra por encima de su apertura.
   - Entrar corto (`SellMarket`) cuando una vela cierra por debajo de su apertura.
2. **Posición existente:**
   - Si una posición larga enfrenta una vela bajista, vender `|Position| + Volume` para cerrar e inmediatamente revertir a una posición corta de tamaño `Volume`.
   - Si una posición corta enfrenta una vela alcista, comprar `|Position| + Volume` para cerrar e inmediatamente revertir a una posición larga de tamaño `Volume`.
3. **Velas neutrales:**
   - Cuando el cierre es igual a la apertura, no se toma acción manual; solo las órdenes protectoras pueden salir de la operación.

### Gestión de riesgos y salidas
- `StartProtection` adjunta un take-profit y un trailing stop medidos en pips. La estrategia multiplica cada valor de pip por `(PriceStep * 10)` para igualar el ajuste de MetaTrader para cotizaciones de 3 y 5 dígitos.
- El trailing stop se activa solo cuando `TrailingStopPips` es mayor que cero; sigue el precio automáticamente una vez que la operación se mueve en la dirección favorable.
- El take-profit cierra la posición cuando se alcanza la distancia configurada. Cualquier nivel protector cancela la orden opuesta tras su ejecución.
- Las reversiones manuales causadas por el color de la vela también aplanan la exposición anterior antes de abrir la nueva posición.

## Parámetros
- `CandleType` – marco temporal de la serie de velas a analizar (por defecto: velas de 15 minutos).
- `TakeProfitPips` – distancia al objetivo de take-profit en pips (por defecto: 50).
- `TrailingStopPips` – distancia del trailing stop en pips (por defecto: 30).
- `MinBars` – recuento mínimo de barras requerido antes de la primera operación (por defecto: 26; la estrategia espera 52 velas completadas).
- `TradeCooldown` – período de espera después de cualquier acción de trading (por defecto: 10 segundos).

Establezca la propiedad `Volume` de la estrategia al tamaño de orden deseado. Cuando el mercado se revierte, la estrategia envía automáticamente suficiente volumen tanto para salir de la posición anterior como para establecer la nueva.

## Notas de implementación
- Solo se procesan las velas completadas (`CandleStates.Finished`). Esto refleja el experto MetaTrader, que dependía de los valores de barra cerrada obtenidos mediante `CopyOpen/CopyClose`.
- El código utiliza la API de alto nivel de StockSharp: `SubscribeCandles` para los datos, `Bind` para procesar las barras entrantes, y `BuyMarket`/`SellMarket` para la ejecución de órdenes.
- Las órdenes protectoras son gestionadas por `StartProtection`, por lo que no es necesario llevar el registro manual de las órdenes stop-limit.
- El cálculo del tamaño de pip `PriceStep * 10` reproduce la lógica de "ajuste de dígitos" de MQL para símbolos cotizados con 3 o 5 decimales.
- Dado que las entradas son activadas por el cuerpo de la vela más reciente, la estrategia tiende a permanecer en el mercado de forma continua, alternando lados cada vez que cambia el color de la vela.

Ajuste las distancias en pips, el período de enfriamiento y el marco temporal para adaptarlos al instrumento que se opera. La configuración predeterminada refleja la muestra MT5 original pero puede optimizarse a través del marco de parámetros de StockSharp.
