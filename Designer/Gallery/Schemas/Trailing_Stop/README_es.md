# Diagrama de la estrategia Trailing Stop (cruce de EMA)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un diagrama de tendencia corto cuyo interés está en la salida y no en la entrada. Dos medias móviles exponenciales eligen el lado, pero la parte de señal nunca cierra una operación: los bloques de modificación de posición solo abren, y es un bloque de protección el que lleva la operación hasta su toma de beneficios o su stop. El interruptor de trailing de ese bloque queda apagado, porque la estrategia original declara una distancia de trailing y no la utiliza.

![schema](schema.svg)

## Resumen de la estrategia

- Una ExponentialMovingAverage rápida y otra lenta se calculan sobre la misma serie de velas.
- Solo se entra desde posición plana, de modo que una operación abierta nunca se invierte ni se amplía.
- Los dos bloques de entrada envían sus propias operaciones al bloque de protección, que coloca el take-profit y el stop-loss como porcentaje del precio de ejecución.
- Ese bloque de protección es la única salida posible; el diagrama no tiene señal de salida propia.

## Reglas de entrada y salida

- **Entrada en largo**: La EMA rápida cruza al alza por encima de la lenta con la posición exactamente en cero. La orden compra un lote y abre un largo.
- **Entrada en corto**: La EMA rápida cruza a la baja por debajo de la lenta con la posición exactamente en cero. La orden vende un lote y abre un corto.
- **Salida**: El bloque de protección cierra la posición con un take-profit del 2% o un stop-loss del 1% sobre el precio de entrada. Hasta que uno de los dos salte, el cruce contrario se ignora, porque entrar exige estar plano.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Fast EMA Length | 6 | Periodo de la media móvil exponencial rápida. |
| Slow EMA Length | 18 | Periodo de la media móvil exponencial lenta. |
| Take Profit, % | 2 | Distancia de la toma de beneficios, en porcentaje del precio de entrada. |
| Stop Loss, % | 1 | Distancia del stop de pérdidas, en porcentaje del precio de entrada. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta ambos bloques de indicador y además aporta el precio que vigila el bloque de protección.
- El bloque de cruce emite verdadero cuando la EMA rápida pasa por encima de la lenta y falso cuando pasa por debajo, así un NO lógico obtiene la señal corta de la misma salida.
- Una sola comparación contra la constante cero basta como control de posición, y ambos bloques de modificación trabajan además en modo de solo apertura.
- Las operaciones propias de los dos bloques de entrada se conectan al bloque de protección: eso es lo que convierte una ejecución en un par de órdenes de beneficio y de pérdida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
