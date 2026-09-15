# Diagrama de la estrategia Account Rules Guard
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama opera los cruces de EMA(120)/EMA(450) sobre velas de un minuto finalizadas y envuelve esa entrada corriente en un supervisor a nivel de cuenta. P&L change, una fórmula, dos comparaciones, un OR lógico, un enclavamiento Flag y una bandera almacenada vigilan en conjunto el resultado monetario combinado de la sesión. En cuanto ese resultado alcanza el límite de pérdida o el objetivo de beneficio, el supervisor cierra la posición, escribe lo ocurrido en el log y bloquea toda entrada posterior hasta que la sesión termina.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas de un minuto finalizadas alimentan dos bloques Indicator, una EMA rápida y una EMA lenta, y dos bloques Crossing leen ese par en ambas direcciones.
- P&L change informa del dinero realizado y del no realizado en la misma actualización, y un bloque Formula suma ambos en un único resultado combinado que se recalcula en cada actualización de P&L.
- Dos bloques Comparison contrastan el resultado combinado con el nivel Max Loss y con el nivel Profit Target, y una Logical condition con el operador OR convierte cualquiera de las dos respuestas en una única señal de regla activada.
- Flag enclava esa señal la primera vez que resulta verdadera. Su entrada de reinicio se deja sin conectar a propósito, de modo que el supervisor es un interruptor de un solo sentido durante el resto de la sesión.
- Una Variable de tipo flag almacena el estado del enclavamiento y lo vuelve a emitir en cada vela, que es lo que un AND lógico necesita para funcionar; una Logical condition con el operador NOT convierte el estado almacenado en el permiso que leen las puertas de entrada.
- Cada puerta de entrada es un AND lógico de tres cosas: un cruce en su dirección, el permiso del supervisor y una comprobación de posición construida con Current position y una Comparison contra cero.
- Position modify abre un Volume a mercado con la condición de abrir posición, de modo que la entrada solo se toma sin posición abierta; el cruce opuesto alimenta un segundo Position modify que cierra lo que esté abierto y deja el diagrama sin posición en lugar de darle la vuelta.
- Cuando la regla salta, un tercer Position modify cierra la posición, una Variable captura el resultado en ese instante, String formatter lo formatea y Notification lo escribe en el log junto con una segunda línea que describe el permiso de negociación de la plataforma.

## Reglas de entrada y salida

- **Entrada en largo**: Una vela finalizada en la que la EMA rápida cruza por encima de la EMA lenta, con el supervisor sin activar y la posición no larga, compra un Volume a mercado. La condición de abrir posición implica que la entrada solo se toma sin posición abierta: la misma señal que llega mientras ya hay una posición abierta se rechaza en lugar de sumarse a ella.
- **Entrada en corto**: Una vela finalizada en la que la EMA rápida cruza por debajo de la EMA lenta, con el supervisor sin activar y la posición no corta, vende un Volume a mercado. Igual que en el lado largo, la condición de abrir posición admite la entrada solo sin posición abierta.
- **Salida**: La salida ordinaria es el cruce opuesto: el bloque Position modify de cierre liquida lo que esté abierto, de modo que el diagrama vuelve a quedarse sin posición y espera un nuevo cruce en lugar de dar la vuelta a la posición. La salida de emergencia es el supervisor: en cuanto el resultado combinado realizado y no realizado alcanza el nivel Max Loss o el nivel Profit Target, la posición se cierra a mercado, el enclavamiento queda fijado, el importe y el permiso de la plataforma se escriben en el log y no se admite ninguna entrada más durante el resto de la sesión.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:01:00 | Marco temporal de la serie de velas. Solo las velas finalizadas impulsan las medias móviles, la reemisión del estado del enclavamiento, el pulso de volumen y la lectura del permiso. |
| Fast EMA Length | 120 | Longitud de la media móvil exponencial rápida. |
| Slow EMA Length | 450 | Longitud de la media móvil exponencial lenta. |
| Max Loss | -5000 | Resultado combinado realizado y no realizado, en la moneda de la cuenta, en el que o por debajo del cual salta el supervisor. Se escribe como número negativo y es deliberadamente amplio: un límite fijado demasiado cerca detiene el diagrama antes de que haya operado lo suficiente como para mostrar algo. |
| Profit Target | 10000 | Resultado combinado realizado y no realizado en el que o por encima del cual salta el supervisor. Alcanzarlo termina la sesión igual que lo hace una pérdida: posición cerrada, enclavamiento fijado, sin más entradas. |
| Volume | 1 | Cantidad fija que usan ambos bloques de entrada. Los dos bloques de cierre toman su importe de la posición abierta e ignoran este valor. |

## Detalles del diagrama

- [P&L change](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) emite juntos el dinero realizado y el no realizado, y la [Formula](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/formula.html) `r + u` los suma en el valor que juzgan ambos bloques [Comparison](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/comparison.html). El bloque permanece en silencio hasta que la cuenta se ha movido realmente, así que el supervisor no puede saltar antes de la primera ejecución, y las dos Variables de límite se disparan con el propio resultado combinado, de modo que ambos lados de cada comparación llegan siempre en la misma actualización.
- El permiso se almacena, no se emite en continuo. [Flag](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/flag.html) emite solo en el instante en que se fija, algo que un AND lógico no puede aprovechar, porque el AND espera un valor en cada entrada y los borra en cuanto dispara. Por eso el estado del enclavamiento vive en una [Variable](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) de tipo flag cuyo valor por defecto es false y cuya entrada Trigger es el flujo de velas: en cada vela vuelve a emitir el estado actual, y una [Logical condition](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) con el operador NOT lo convierte en el permiso de entrada.
- El operador OR sobre la señal de regla activada es una elección deliberada: a diferencia del AND, no espera un valor en cada entrada, por lo que cualquiera de los dos límites por sí solo puede levantarla. Además emite una respuesta falsa en cada actualización tranquila, lo que no cuesta nada aguas abajo: Flag ignora un disparo falso, las Variables de captura lo ignoran y [Position modify](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) se niega a actuar sobre él, de modo que una respuesta negativa nunca envía una orden.
- Los bloques de cierre usan la condición de cerrar posición y no necesitan ninguna entrada de volumen: el importe se toma de la posición abierta. Los bloques de entrada conservan su propio Volume, y [Current position](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/positions/current.html) comparada contra cero da a cada puerta las mismas comprobaciones de no estar largo y no estar corto que enuncia la regla de entrada.
- [Is trade allowed](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/time/trade_allow.html) lee el permiso propio de la plataforma en cada vela, y se mantiene fuera de las puertas de entrada a propósito: sobre histórico grabado responde "no permitido" durante toda la sesión, así que una puerta construida sobre él nunca se abriría y el diagrama no operaría en absoluto. Su respuesta se captura en una Variable y [String formatter](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) la formatea en la segunda línea de [Notification](https://doc.stocksharp.com/en/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html), que es donde le corresponde estar: explica el estado de la plataforma en el momento en que saltó la regla en lugar de silenciar el diagrama. Notification está configurado con el tipo log, el único tipo que se entrega mientras se reproduce el histórico.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
