# Estrategia de Hoop Master
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Hoop Master es un sistema de ruptura pendiente que mantiene continuamente dos órdenes stop alrededor del precio actual. El asesor experto de MetaTrader 5 original coloca un buy stop por encima del mercado y un sell stop por debajo del mercado. Cuando un lado se activa, la orden opuesta se cancela y ambos lados se recrean con un volumen mayor. El port de StockSharp sigue la misma idea gestionando órdenes stop y un dimensionamiento martingala opcional dentro de una sola clase de estrategia.

La estrategia también puede adjuntar órdenes de stop-loss y take-profit protectoras a cualquier posición abierta. Un módulo de trailing stop mueve gradualmente el stop protector cuando el mercado avanza en la dirección de la operación.

## Lógica de trading

1. En cada vela completada, la estrategia recalcula los niveles de colocación para los stops de ruptura.
2. Si no hay posición abierta, tanto un buy stop como un sell stop se registran a una distancia configurable en pips desde el bid/ask actual.
3. Cuando cualquier stop pendiente se llena, el stop opuesto se elimina. Nuevos stops de ruptura se envían inmediatamente usando el doble del volumen base.
4. Después de que se abre una operación, la estrategia crea órdenes independientes de stop-loss y take-profit. Un motor de trailing puede mover el stop hacia el precio una vez que el movimiento sea lo suficientemente grande.
5. Cuando la posición se cierra, todas las órdenes de protección se cancelan y las órdenes de ruptura se re-inicializan con el volumen base en la siguiente señal.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| **Candle Type** | Tipo de datos de vela usado para la lógica barra a barra. |
| **Order Volume** | Volumen base para cada orden de ruptura. El paso martingala usa el doble de esta cantidad. |
| **Stop Loss (pips)** | Distancia en pips entre el precio de entrada y la orden stop protectora. Establecer en 0 para deshabilitar. |
| **Take Profit (pips)** | Distancia en pips entre el precio de entrada y la orden objetivo protectora. Establecer en 0 para deshabilitar. |
| **Trailing Stop (pips)** | Distancia usada al mover el trailing stop. Establecer en 0 para deshabilitar el trailing. |
| **Trailing Step (pips)** | Mejora de precio mínima (en pips) requerida antes de que se actualice el trailing stop. |
| **Indent (pips)** | Offset, medido en pips, añadido por encima del ask y por debajo del bid al colocar stops de ruptura. |

## Detalles de gestión de órdenes

- La estrategia rastrea continuamente las mejores cotizaciones de bid/ask. Cuando las cotizaciones no están disponibles, recurre al último precio de operación o al cierre de vela.
- Todas las órdenes se alinean al paso de precio del instrumento para evitar precios inválidos.
- Las órdenes de stop y take-profit protectoras se reemplazan cada vez que aparece una nueva posición.
- El trailing solo funciona cuando tanto la distancia de trailing como los parámetros de paso están por encima de cero. El stop se mueve en la dirección de la operación cuando la mejora deseada es suficientemente grande.

## Notas

- Asegúrate de que el bróker o simulador conectado soporte órdenes stop y límite para el instrumento seleccionado.
- El paso martingala puede aumentar la exposición rápidamente. Ajusta el volumen base para mantenerse dentro de límites de riesgo aceptables.
- La estrategia espera recibir datos de Nivel1 (bid/ask) junto con datos de velas para que los precios de ruptura puedan calcularse con precisión.
