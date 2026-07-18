# Estrategia de Entrada Única por Línea de Orden
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Line Order es una traducción del script MQL4 "LineOrder" (10715). La estrategia abre una posición cuando el precio de mercado alcanza una línea de entrada predefinida y luego gestiona la posición con stop-loss, take-profit y un trailing stop opcional.

## Parámetros

- `Entry Price` – nivel de precio que activa una posición.
- `Stop Loss (pips)` – distancia desde la entrada hasta el stop loss inicial.
- `Take Profit (pips)` – distancia desde la entrada hasta el take profit.
- `Trailing Stop (pips)` – distancia opcional del trailing stop. Cuando se establece en cero, el trailing se desactiva.
- `Candle Type` – tipo de velas utilizadas para el procesamiento.

## Lógica de Operación

1. La estrategia se suscribe a la serie de velas seleccionada.
2. Cuando una vela completada cierra por encima del precio de entrada, se abre una posición larga. Cuando cierra por debajo del precio de entrada, se abre una posición corta.
3. Tras la entrada, los niveles de stop-loss y take-profit se calculan usando el paso de precio del instrumento.
4. Si el trailing stop está habilitado, el nivel del stop se mueve en la dirección del trade.
5. La posición se cierra cuando el precio alcanza el nivel de stop-loss o take-profit.

Esta es una adaptación simplificada del script MQL original, enfocada en la ejecución automatizada de órdenes en una línea definida por el usuario.
