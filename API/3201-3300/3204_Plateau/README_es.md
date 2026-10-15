# Estrategia de Plateau
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Plateau es una conversión del asesor experto original de MetaTrader 5. Combina un par de medias móviles ponderadas linealmente con Bollinger Bands para detectar posibles reversiones cuando el precio opera cerca de la banda inferior.

## Idea de trading

* Calcular medias móviles rápidas y lentas usando el método de suavizado seleccionado y la fuente de precio.
* Construir Bollinger Bands alrededor de la misma serie de precios.
* Cuando la media rápida cruza por encima de la lenta mientras la vela anterior cerró por debajo de la banda inferior, abrir una posición long.
* Cuando la media rápida cruza por debajo de la lenta mientras la vela anterior cerró por encima de la banda inferior, abrir una posición short.
* Opcionalmente invertir las señales si el interruptor `Reverse` está habilitado.

## Gestión de órdenes

* Las posiciones pueden dimensionarse con un lote fijo o arriesgando un porcentaje del valor del portfolio por operación.
* Los niveles de stop-loss y take-profit se expresan en pips y se adjuntan inmediatamente después de que se llena la orden de mercado.
* Se puede activar un trailing stop cuando tanto la distancia de trailing como el paso son positivos.
* Cuando `Close Opposite` está habilitado, la estrategia cierra automáticamente la posición opuesta antes de entrar en una nueva operación.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| Stop Loss | Distancia del stop-loss en pips. |
| Take Profit | Distancia del take-profit en pips. |
| Trailing Stop | Distancia del trailing stop en pips. |
| Trailing Step | Incremento mínimo (en pips) necesario para mover el trailing stop. |
| Money Mode | Elegir entre lote fijo y dimensionamiento por porcentaje de riesgo. |
| Lot / Risk | El tamaño de lote fijo o el porcentaje de riesgo dependiendo del modo de dinero seleccionado. |
| Fast MA / Slow MA | Períodos para el par de medias móviles. |
| MA Shift | Desplazamiento horizontal aplicado a ambas medias móviles. |
| MA Method | Algoritmo de suavizado de la media móvil. |
| MA Price | Fuente de precio utilizada para los cálculos de media móvil. |
| Bands Period | Período de promediado para Bollinger Bands. |
| Bands Shift | Desplazamiento horizontal aplicado a los valores de Bollinger Bands. |
| Bands Deviation | Multiplicador de desviación estándar para Bollinger Bands. |
| Bands Price | Fuente de precio utilizada para los cálculos de Bollinger Bands. |
| Reverse | Invertir la lógica de señal long y short. |
| Close Opposite | Cerrar una posición existente en la dirección opuesta antes de abrir una nueva. |
| Verbose Log | Imprimir información detallada de ejecución en el registro. |
| Candle Type | Serie de datos de velas utilizada para los cálculos de indicadores. |

## Notas

* El tamaño de pip se ajusta automáticamente a instrumentos con tres o cinco dígitos decimales para que coincida con el comportamiento del experto original.
* Cuando el trailing stop está habilitado, el paso de trailing debe ser estrictamente positivo; de lo contrario, la estrategia arroja un error al inicio.
* El dimensionamiento de posición basado en riesgo requiere tanto una distancia de stop-loss válida como datos de valoración del portfolio. Cuando no están disponibles, la estrategia recurre al volumen predeterminado.
