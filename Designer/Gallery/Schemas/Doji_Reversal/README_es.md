# Diagrama de la estrategia de reversión con doji
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un doji es una vela que abre y cierra casi al mismo precio: compradores y vendedores se anularon durante toda la barra. El diagrama mide esa indecisión como la proporción entre cuerpo y rango completo y deja que los dos cierres anteriores al doji decidan el lado, porque el doji por sí solo no dice nada de la dirección. La única salida es una media móvil simple.

![schema](schema.svg)

## Resumen de la estrategia

- Un bloque de fórmula calcula el cuerpo menos el rango multiplicado por el umbral: un resultado negativo significa que el cuerpo es menor que la fracción permitida de la vela.
- Escribir la prueba como multiplicación en vez de división reproduce además la protección del código original: en una vela donde el máximo iguala al mínimo se compara cero contra cero y no se reconoce ningún doji.
- Dos bloques de valor anterior leen los cierres de una y dos velas atrás: una caída entre ellos se toma como tramo bajista y se compra, una subida como tramo alcista y se vende.
- La estrategia original bloquea además todas las señales durante varios cientos de barras tras una ejecución; aquí no existe un bloque contador de barras, así que esa pausa se omite y se documenta.

## Reglas de entrada y salida

- **Entrada en largo**: La vela recién cerrada es un doji, el cierre de una vela atrás es menor que el de dos velas atrás y la posición es cero. La orden compra un lote y abre un largo.
- **Entrada en corto**: La vela recién cerrada es un doji, el cierre de una vela atrás es mayor que el de dos velas atrás y la posición es cero. La orden vende un lote y abre un corto.
- **Salida**: Un largo se cierra con un bloque de modificación de posición en modo cierre cuando una vela cierra por debajo de la media móvil; un corto se cierra cuando una vela cierra por encima. La estrategia de origen no tiene stop loss ni take profit, y este diagrama tampoco.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Doji Threshold | 0.1 | Proporción máxima entre cuerpo y rango completo con la que una vela sigue contando como doji. |
| SMA Length | 20 | Periodo de la media móvil simple que cierra las operaciones. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas de todo el diagrama; el original usa velas de un minuto y aquí se ajusta al histórico de cinco minutos incluido en la galería. |

## Detalles del diagrama

- El bloque de velas alimenta cuatro conversores de apertura, máximo, mínimo y cierre, además de la media móvil.
- Los cuatro precios y la constante de umbral se encuentran en un único bloque de fórmula, y una comparación contra cero convierte su resultado en el indicador de doji.
- El precio de cierre entra también en dos bloques de valor anterior, cuyas salidas se comparan entre sí para dar la dirección del último tramo.
- Cada Y lógica une el indicador de doji, una condición de dirección y el control de posición, y dispara una entrada; los dos bloques de cierre se disparan directamente desde las comparaciones con la media móvil.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
