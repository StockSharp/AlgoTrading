# USD/CHF CCI Estrategia de parada de canal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de parada de canal CCI USD/CHF** es una StockSharp implementación de alto nivel del MetaTrader 4 asesores expertos `UsdChf_new`. La estrategia escucha las rupturas del índice de canales de productos básicos (CCI) en el marco temporal del cuarto semestre y despliega órdenes stop pendientes por encima o por debajo del precio actual. Una vez que se completa una orden, la posición está protegida por las mismas reglas de administración de dinero basadas en pips utilizadas en el robot original: un stop loss fijo, cancelación opcional de órdenes pendientes obsoletas, reubicación del punto de equilibrio y gestión de trailing stop.

Esta conversión mantiene el flujo de ejecución original pero adopta el flujo de trabajo idiomático StockSharp: suscripciones de velas, vinculaciones de indicadores y asistentes de órdenes de alto nivel (`BuyStop`, `SellStop`, `BuyMarket`, `SellMarket`). Todas las distancias de riesgo todavía están configuradas en pips para que los usuarios de Forex sigan siendo familiares.

## Lógica de trading

1. **Indicador y señales**
   - Calcule un CCI con el período configurado en velas H4 terminadas.
   - Supervise los límites del canal: `+CCI Channel` y `-CCI Channel`.
   - Detecta cruces del valor actual con el valor anterior para generar señales.
     - Cruzar **hacia arriba** a través de `-CCI Channel` prepara una **parada de compra** por encima del precio.
     - Cruzar **hacia abajo** a través de `+CCI Channel` prepara una **parada de venta** por debajo del precio.
2. **Órdenes pendientes**
   - Las órdenes stop se compensan desde el cierre de la vela en `Entry Indent (pips)` y se redondean al paso del instrumento.
   - Sólo puede haber una orden pendiente activa a la vez. Crear uno nuevo cancela el lado opuesto.
   - Si el mercado se aleja más de `Cancel Distance (pips)`, la orden pendiente se cancela para evitar perseguir el precio.
3. **Gestión de posiciones**
   - Las posiciones ocupadas heredan la distancia de stop loss original.
   - Cuando la operación gana al menos `Break Even (pips)`, el stop de protección se mueve al precio de entrada.
   - Después de que la ganancia excede `Trailing Stop (pips)`, el stop sigue el precio manteniendo la brecha configurada.
   - Los cruces opuestos de CCI fuerzan una salida de posición y colocan una nueva orden de parada en la nueva dirección.

## Parámetros

| Parámetro | Descripción | Predeterminado | Optimizable |
|-----------|-------------|---------|-------------|
| `CandleType` | Serie de velas utilizada para los cálculos CCI (predeterminado H4). | plazo de 4 horas | No |
| `CciPeriod` | CCI período promedio. | 73 | si |
| `CciChannel` | Nivel absoluto CCI que forma los límites del canal. | 120 | si |
| `EntryIndentPips` | Distancia (en pips) entre el precio de mercado y la orden stop pendiente. | 30 | si |
| `StopLossPips` | Distancia inicial de stop loss en pips. | 95 | si |
| `CancelDistancePips` | Gap máximo antes de cancelar órdenes pendientes. | 30 | si |
| `TrailingStopPips` | Distancia de trailing stop una vez activado. | 110 | si |
| `BreakEvenPips` | Beneficio requerido antes de que la parada se mueva al nivel de entrada. | 60 | si |

Todas las distancias de pips se convierten en compensaciones de precios utilizando el instrumento `PriceStep` y `Decimals`. Para los símbolos Forex de 3/5 dígitos, el pip equivale a diez pasos de precio; de lo contrario, equivale a un solo paso.

## Notas de uso

1. Adjunte la estrategia a un valor USD/CHF (o cualquier instrumento donde la gestión de riesgos basada en pips sea relevante).
2. Establezca el volumen de operaciones deseado a través de la propiedad base `Strategy.Volume`.
3. Opcionalmente, ajuste los parámetros basados en pips para que coincidan con las especificaciones del contrato del corredor.
4. Ejecute pruebas retrospectivas en Designer/Tester para validar el comportamiento antes de publicarlo.

## Notas de conversión

- El experto MetaTrader recorrió los grupos de pedidos sin procesar. En StockSharp la estrategia almacena referencias a las órdenes pendientes activas y en su lugar utiliza ayudas de cancelación de alto nivel.
- El stop loss, el punto de equilibrio y el seguimiento se implementan mediante salidas de mercado explícitas porque la modificación de las órdenes del corredor no forma parte del API de alto nivel.
- Todos los comentarios en línea se tradujeron al inglés y se ampliaron para mayor claridad.
