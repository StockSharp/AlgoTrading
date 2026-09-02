# Diagrama de la estrategia del cambio de mes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama opera un efecto de calendario en lugar de una figura de precio: mantiene una posición larga sobre la frontera entre dos meses y se queda plano a mitad de mes. No hay ningún indicador; la única entrada es la fecha que lleva cada vela cerrada.

![schema](schema.svg)

## Resumen de la estrategia

- Un conversor extrae el número de día de la hora de apertura de la vela y una fórmula corta lo convierte en la distancia al borde de mes más cercano: min(day - 1, 31 - day).
- Un único umbral define toda la ventana: mientras esa distancia sea menor o igual, la fecha cuenta como cambio de mes; por encima, como mitad de mes.
- El original cuenta días de negociación y salta los fines de semana; un diagrama no tiene bucles, así que se usan días naturales y la ventana queda simétrica respecto a la frontera del mes. En un mes de 31 días cubre los seis primeros y los seis últimos, en un mes corto uno o dos menos.
- La estrategia es solo larga, por lo que el control de la posición elige entre abrir y cerrar, y no existe rama corta alguna.
- La pausa de 10 barras entre operaciones del original se omite: con una ventana que dura varios días y una entrada limitada por la posición, no cambia nada.

## Reglas de entrada y salida

- **Entrada en largo**: La distancia al borde de mes es menor o igual que la ventana y la posición no es larga. La orden compra el volumen fijo y abre el largo que debe atravesar la frontera del mes.
- **Entrada en corto**: No hay entrada corta. La estrategia solo mantiene una posición larga o ninguna, igual que el original.
- **Salida**: La distancia al borde de mes es mayor que la ventana y la posición es larga. El bloque de cierre envía una orden a mercado por el tamaño de la posición abierta, de modo que el diagrama pasa plano la mitad del mes. No hay stop de pérdidas ni toma de beneficios.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Window, days | 5 | Semiancho de la ventana de calendario, en días: la fecha cuenta como cambio de mes mientras no se aleje más de este valor del primer o del último día. |
| Volume | 1 | Volumen de la orden, en lotes, con el que se abre el largo; la salida cierra el tamaño que haya abierto. |
| Candles | 00:30:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta un conversor tipado como vela cuya ruta de propiedad es OpenTime.Day, lo que devuelve el día natural como número simple.
- El bloque de fórmula pliega ese número en la distancia al borde de mes más cercano, así que un solo umbral cubre el final de un mes y el comienzo del siguiente.
- Dos bloques de comparación parten el calendario en la ventana y su complemento; otros dos comparan la posición con una constante cero.
- Cada Y lógica une una condición de calendario con una de posición: la primera dispara un bloque de apertura y la segunda un bloque de cierre que toma su tamaño de la propia posición.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
