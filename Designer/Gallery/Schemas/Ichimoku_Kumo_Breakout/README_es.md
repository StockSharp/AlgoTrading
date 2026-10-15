# Diagrama de la estrategia de ruptura de la nube Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El nombre remite a la nube de Ichimoku, pero la estrategia que hay detrás de este diagrama opera en realidad el par de líneas más rápido: Tenkan-sen frente a Kijun-sen. Ambas son el punto medio entre el máximo y el mínimo de su periodo, así que su cruce ya constituye una señal de tendencia compacta, y la nube queda fuera de la decisión de forma deliberada.

![schema](schema.svg)

## Resumen de la estrategia

- Un solo bloque Ichimoku construye las cinco líneas; dos conversores extraen únicamente Tenkan-sen y Kijun-sen, y las líneas de la nube no intervienen en las reglas.
- El bloque de cruce dispara solo en la vela en la que Tenkan-sen cruza realmente a Kijun-sen, de modo que una tendencia que simplemente dura no genera órdenes repetidas.
- Cada entrada se combina con la posición actual, que es lo que impide al diagrama acumular lotes en un lado que ya mantiene.

## Reglas de entrada y salida

- **Entrada en largo**: Tenkan-sen cruza por encima de Kijun-sen y la posición no es larga. La orden compra el volumen fijo: abre un largo desde plano o cierra un corto existente.
- **Entrada en corto**: Tenkan-sen cruza por debajo de Kijun-sen y la posición no es corta. La orden vende el volumen fijo: abre un corto desde plano o cierra un largo existente.
- **Salida**: No hay bloque de salida propio ni stop de protección: como todas las órdenes usan el mismo volumen, el cruce contrario devuelve la posición a plano en lugar de invertirla, y el otro lado solo se abre en el cruce posterior.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Tenkan period | 9 | Periodo de Tenkan-sen, el punto medio entre el máximo y el mínimo de ese número de velas. |
| Kijun period | 26 | Periodo de Kijun-sen, construido igual pero sobre una ventana más larga. |
| Senkou Span B period | 52 | Periodo de Senkou Span B; no forma parte de las reglas y solo afecta a cuántas velas necesita el indicador para formarse. |
| Volume | 1 | Volumen de la orden, en lotes; se usa igual para abrir y para cerrar. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta un único bloque de indicador Ichimoku, y dos conversores leen los valores de Tenkan y Kijun del valor del indicador complejo.
- Ambas líneas se encuentran en el bloque de cruce, cuya salida es la señal larga; un NO lógico sobre ella da la señal corta.
- El bloque de posición se compara dos veces con una constante cero, lo que produce los filtros Posición <= 0 y Posición >= 0.
- Cada Y lógica une una señal de cruce con un filtro de posición y dispara un bloque de modificación de posición; ambos envían órdenes a mercado y toman el volumen de una constante compartida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
