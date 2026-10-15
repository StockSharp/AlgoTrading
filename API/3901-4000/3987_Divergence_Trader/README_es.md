# Trader de divergencia (conversión clásica)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el comportamiento del MetaTrader asesor experto 4 **Divergence Trader** dentro del StockSharp nivel alto API. Se calculan dos medias móviles simples sobre el precio de la vela seleccionada (abierta de forma predeterminada). El sistema monitorea cómo cambia la distancia entre los promedios rápido y lento de una barra a la siguiente:

* Cuando el diferencial se amplía al alza y el valor de divergencia se mantiene entre el *umbral de compra* y el *umbral de estancia fuera*, se abre una posición larga o se cubre una posición corta existente.
* Cuando el diferencial se amplía a la baja dentro de los umbrales reflejados, se ingresa una posición corta o se cierra una operación larga existente.

Sólo se utilizan velas completas, que coinciden con el procesamiento barra por barra del asesor experto original. Todas las reglas de administración se implementan con llamadas de alto nivel impulsadas por eventos (`BuyMarket`/`SellMarket`).

## Reglas comerciales

1. Suscríbase al tipo de vela configurado y calcule dos SMA con períodos *Rápido SMA* y *Lento SMA*.
2. Calcule el diferencial actual (`fast - slow`) y compárelo con el diferencial anterior para obtener el valor de divergencia.
3. Ingrese largo si la divergencia es positiva, mayor o igual a *Umbral de compra* y menor o igual a *Umbral de permanencia fuera*.
4. Ingrese short si la divergencia es negativa, menor o igual a `-Buy Threshold` y mayor o igual a `-Stay Out Threshold`.
5. Invierta una posición existente cada vez que aparezca una señal opuesta.
6. Restrinja las nuevas entradas a la ventana de hora local entre *Hora de inicio* y *Hora de finalización* (se admite el ajuste después de la medianoche).

## Gestión de riesgos

* Los niveles fijos opcionales de *Take Profit (pips)* y *Stop Loss (pips)* se monitorean en los máximos y mínimos de las velas.
* El *Trigger de punto de equilibrio (pips)* mueve el stop a `entry ± Break-Even Buffer` una vez que la posición gana el número especificado de pips.
* El *Trailing Stop (pips)* sigue el precio más favorable una vez que la operación genera ganancias. La configuración 9999 desactiva el trailing stop, reflejando el valor predeterminado original EA.
* La gestión de la cesta cierra todas las exposiciones abiertas cuando las pérdidas y ganancias no realizadas alcanzan el *beneficio de la cesta* o caen por debajo de `-Basket Loss` en la moneda de la cuenta.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `Order Volume` | Volumen utilizado cuando se abre una nueva posición. |
| `Fast SMA` / `Slow SMA` | Períodos para las dos medias móviles simples. |
| `Applied Price` | El componente de vela se trasladó a ambos promedios móviles. |
| `Buy Threshold` | Límite de divergencia inferior que permite operaciones largas. |
| `Stay Out Threshold` | Límite de divergencia superior por encima del cual no se realizan nuevas operaciones. |
| `Take Profit (pips)` / `Stop Loss (pips)` | Salidas duras opcionales medidas en pips. |
| `Trailing Stop (pips)` | Distancia de seguimiento que se aplica después de que la operación se vuelve rentable. |
| `Break-Even Trigger (pips)` | Beneficio en pips requerido antes de mover el stop al punto de equilibrio. |
| `Break-Even Buffer (pips)` | Se agregó un amortiguador adicional al punto de equilibrio. |
| `Basket Profit` / `Basket Loss` | Límites de capital global en la moneda de la cuenta. |
| `Start Hour` / `Stop Hour` | Ventana de sesión de negociación local. |
| `Candle Type` | Plazo utilizado para la suscripción y los cálculos de velas. |

## Notas de uso

* Adjunte la estrategia a un valor y establezca el tipo de vela que coincida con el período de tiempo del gráfico original.
* Asegúrese de que las propiedades `PriceStep`/`StepPrice` del instrumento estén configuradas para que los controles basados en pips funcionen correctamente.
* Para deshabilitar funciones como el trailing stop o el cambio de equilibrio, mantenga sus parámetros en el valor centinela heredado (9999) o cero.
