# Diagrama de la estrategia base de lanzamiento aleatorio de moneda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama opera deliberadamente sin una señal de mercado. Cada vez que llega una vela de cuatro horas finalizada mientras la posición está plana, un valor aleatorio elige entre una entrada larga y una corta. La posición se mantiene durante diez velas finalizadas adicionales, se cierra a mercado y el ciclo puede comenzar de nuevo en la vela siguiente. Es una referencia educativa para comparar sistemas basados en reglas, no una estrategia de trading en vivo.

![schema](schema.svg)

## Descripción general de la estrategia

- Un único flujo de velas de cuatro horas, limitado a velas finalizadas, marca tanto las decisiones aleatorias como el contador del período de mantenimiento.
- El bloque Random produce un valor entre cero y uno. Un umbral de 0.5 divide el rango en dos direcciones mutuamente excluyentes.
- La posición actual se captura cuando llega cada vela finalizada y se compara con cero. Ambos bloques de entrada también usan la condición Open position, por lo que una operación solo puede comenzar si el diagrama estaba sin posición al inicio de la vela.
- La operación de entrada activa un bloque N values, que cuenta diez velas finalizadas posteriores antes de permitir que se cierre la posición.
- El diagrama no usa indicadores, stop loss ni take profit. Su secuencia aleatoria no tiene una semilla configurada dentro del diagrama y puede variar entre ejecuciones.

## Reglas de entrada y salida

- **Entrada larga**: El valor aleatorio está por debajo del umbral de la moneda y la posición está plana. El diagrama compra a mercado el volumen configurado.
- **Entrada corta**: El valor aleatorio es igual o superior al umbral de la moneda y la posición está plana. El diagrama vende a mercado el volumen configurado.
- **Salida**: Una vez ejecutada una entrada, el diagrama cuenta diez velas de cuatro horas finalizadas posteriores. A continuación, el bloque N values activa el cierre a mercado de toda la posición. La vela de salida no abre otra operación; la siguiente vela finalizada es la primera oportunidad para volver a entrar.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|---|---|---|
| Hold Bars | 10 | Número de velas finalizadas contadas después de ejecutar una entrada y antes de cerrar la posición; el valor debe ser mayor que cero. |
| Volume | 1 | Volumen de las órdenes de entrada y salida, en lotes. El mismo volumen configurado se utiliza para abrir y reducir la posición. |
| Coin Threshold | 0.5 | Los valores aleatorios por debajo de este nivel seleccionan una entrada larga; los valores iguales o superiores seleccionan una entrada corta. |
| Candles | 04:00:00 | Marco temporal de cuatro horas utilizado para las decisiones de entrada y el recuento del período de mantenimiento; solo se procesan velas finalizadas. |

## Detalles del diagrama

- La salida de [Candles](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) alimenta el bloque [Random](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/random.html), activa la captura de la posición, entra en el socket Input del bloque [N values](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) y llega al panel del gráfico.
- Una [Comparación](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) comprueba si el valor aleatorio es al menos igual al umbral de la moneda. Esa señal selecciona la rama corta, mientras que una [Condición lógica](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) en modo NOT produce la rama larga.
- La salida del bloque [Position](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/current.html) alimenta una Variable activada que captura la posición al inicio de la vela. La captura se compara con una constante cero compartida y su señal de posición plana se combina con la señal de dirección en cada bloque AND de entrada. Esta captura evita que la vela de salida abra una nueva posición inmediatamente después de que se ejecute el cierre.
- Ambos bloques de entrada [Modify position](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) usan órdenes de mercado con la condición Open position y reciben su volumen de una constante compartida.
- Las salidas MyTrade de los bloques de entrada larga y corta alimentan el socket Trigger del bloque N values. Los activadores posteriores se ignoran mientras está activo su recuento de diez velas.
- La salida de N values activa dos bloques Modify position en modo Reduce only. El bloque de venta solo puede reducir una posición larga y el bloque de compra solo puede reducir una posición corta; ambos reciben el volumen compartido, por lo que únicamente la rama aplicable registra una salida.
- El [panel del gráfico](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recibe el flujo de velas y las operaciones generadas por los dos bloques de entrada y los dos bloques de salida.

## Uso

Importe el archivo `.json` en Designer y ejecútelo en el backtester con datos históricos; después, compare sus resultados con diagramas basados en reglas sobre el mismo instrumento y período. Use este ejemplo como referencia educativa, no como sistema de trading en vivo.
