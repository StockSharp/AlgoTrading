# Estrategia Autotrader Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Autotrader Momentum** es una conversión del asesor experto de MetaTrader 5 *Autotrader Momentum (edición de barabashkakvn)*. El algoritmo evalúa el momentum reciente comparando el precio de cierre de la barra de monitoreo con el precio de cierre de una barra de referencia histórica. Cuando se detecta un cambio de momentum alcista, la estrategia compra; cuando aparece un cambio bajista, vende. Todas las órdenes se ejecutan al precio de mercado usando la API de trading de alto nivel de StockSharp.

La implementación mantiene el enfoque original en el control de riesgo basado en puntos. Las distancias de stop-loss, take-profit y trailing-stop se definen en pips y se traducen automáticamente en desplazamientos de precio basados en el `PriceStep` del instrumento. Se conserva el soporte para cotizaciones de tres y cinco decimales aplicando el mismo ajuste de 10x usado en el código MQL. La lógica de trailing se evalúa en cada vela finalizada antes de considerar nuevas entradas, asegurando que la gestión de riesgo refleje el comportamiento del EA de priorizar salidas protectoras.

## Lógica de trading
1. Suscribirse al `CandleType` configurado y procesar solo las velas finalizadas, replicando la lógica de "nueva barra" del EA original.
2. Mantener una ventana deslizante de precios de cierre de tamaño `max(CurrentBarIndex, ComparableBarIndex) + 1`.
3. Comparar el cierre de la barra monitoreada (`CurrentBarIndex`, predeterminado 0) con el cierre de la barra histórica (`ComparableBarIndex`, predeterminado 15).
4. Si el cierre monitoreado es mayor que el cierre de referencia, cerrar cualquier exposición corta y comprar el volumen de trading configurado.
5. Si el cierre monitoreado es menor que el cierre de referencia, cerrar cualquier exposición larga y vender el volumen de trading configurado.
6. Cada entrada recalcula el precio promedio de entrada y actualiza los niveles de stop-loss, take-profit y trailing-stop.

Dado que las estrategias de StockSharp trabajan con una posición neta, las reversiones combinan el volumen necesario para cerrar la exposición opuesta con el volumen base configurado. Esto coincide con el comportamiento MQL que primero cerraba el lado opuesto y luego abría una nueva orden del tamaño solicitado.

## Parámetros
- `CandleType` – Marco temporal usado para la comparación de precios. Predeterminado: 1 hora.
- `TradeVolume` – Volumen base de la orden de mercado. Se aplica en cada señal además de cualquier volumen necesario para revertir una posición existente.
- `StopLossPips` – Distancia de stop protector en pips. Establecer en 0 para deshabilitar el stop-loss fijo.
- `TakeProfitPips` – Distancia del objetivo de beneficio en pips. Establecer en 0 para deshabilitar el take-profit fijo.
- `TrailingStopPips` – Distancia mantenida por el trailing stop. Establecer en 0 para deshabilitar el trailing.
- `TrailingStepPips` – Movimiento favorable mínimo requerido antes de avanzar el trailing stop. Debe ser positivo cuando el trailing está habilitado.
- `CurrentBarIndex` – Índice de la vela de monitoreo (0 = barra finalizada más reciente).
- `ComparableBarIndex` – Índice de la barra histórica usada para comparación de momentum.

Todos los ajustes basados en pips se convierten en desplazamientos de precio usando el `PriceStep` del instrumento. Si el step representa tres o cinco dígitos decimales, el desplazamiento se multiplica por 10 para reproducir la definición de pip de MetaTrader.

## Gestión de riesgo
- **Stops y objetivos fijos:** Cuando `StopLossPips` o `TakeProfitPips` son mayores que cero, la estrategia mantiene los niveles de precio correspondientes relativos al precio de entrada promediado.
- **Trailing Stop:** Habilitado cuando tanto `TrailingStopPips` como `TrailingStepPips` son positivos. La lógica de trailing mueve el stop protector solo después de que el precio se haya movido al menos `TrailingStopPips + TrailingStepPips` desde el precio de entrada promediado, replicando el requisito del EA que aseguraba que el movimiento fuera suficientemente grande antes de ajustar el stop.
- **Reinicio de estado:** Cada vez que la posición vuelve a cero—ya sea mediante salidas impulsadas por la estrategia o intervención externa—el estado de riesgo almacenado en caché se borra para evitar niveles obsoletos de stop o take-profit.

## Notas de implementación
- La estrategia se basa exclusivamente en la API de mercado de alto nivel de StockSharp (`BuyMarket`, `SellMarket`) y evita colecciones de indicadores para mantenerse fiel a las pautas de conversión.
- Los precios de cierre se almacenan en una lista deslizante simple para que `CurrentBarIndex` y `ComparableBarIndex` puedan cambiarse en tiempo de ejecución sin necesidad de reinicio.
- Dado que StockSharp opera sobre una posición neta, los niveles de stop-loss y take-profit se rastrean para la exposición agregada. Cuando se agregan órdenes adicionales en la misma dirección, el código recalcula un precio de entrada promedio ponderado por volumen antes de actualizar los niveles de riesgo.
- Los ajustes de trailing-stop y las salidas protectoras se procesan antes de las nuevas señales en cada vela, evitando que se evalúen nuevas entradas cuando ya se ha emitido una salida para esa barra.

## Referencia de la estrategia original
- **Fuente:** `MQL/22409/Autotrader Momentum.mq5`
- **Autor:** barabashkakvn (comunidad MetaTrader)
