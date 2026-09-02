# Diagrama de la estrategia del efecto día de la semana
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El calendario decide la dirección y la media móvil decide el momento. A principios de semana el diagrama puede comprar, a finales de semana puede vender, y en ambos casos espera a que el precio de cierre quede del lado correspondiente de una media móvil simple antes de actuar. El día de la semana se lee directamente de la vela, así que no hay que arrastrar ningún estado de una vela a la siguiente.

![schema](schema.svg)

## Resumen de la estrategia

- Un conversor extrae el día de la semana de la hora de apertura de la vela como número, donde el domingo es cero y el sábado es seis.
- Cada ventana del calendario la forman dos comparaciones: de lunes a martes para el lado largo y de jueves a viernes para el corto, con los límites expuestos como parámetros para poder mover o ampliar la ventana.
- Una media móvil simple del precio de cierre confirma la dirección; el calendario por sí solo nunca abre una operación.
- La posición actual interviene en ambas entradas, de modo que el diagrama nunca aumenta una operación que ya tiene.

## Reglas de entrada y salida

- **Entrada en largo**: La vela pertenece a la ventana de principio de semana, su cierre está por encima de la media móvil simple y la posición está plana. La orden compra el volumen compartido a mercado.
- **Entrada en corto**: La vela pertenece a la ventana de final de semana, su cierre está por debajo de la media móvil simple y la posición está plana. La orden vende el volumen compartido a mercado.
- **Salida**: Un cierre de vuelta por debajo de la media cierra un largo y un cierre de vuelta por encima cierra un corto, ambos mediante bloques de modificación de posición en modo cierre. Como un bloque de cierre no hace nada si la posición ya está plana, esto reproduce la prueba de cruce del original sin bloques adicionales. El original tiene dos contadores que el diagrama no puede mantener entre velas y se han eliminado los dos: la pausa de trescientas barras tras cada operación y la regla que prohíbe una segunda entrada el mismo día de la semana. Sin ellos el diagrama vuelve a entrar en cuanto el precio regresa al lado correcto de la media dentro de la misma ventana, así que opera bastante más que el original.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| MA Period | 20 | Periodo de la media móvil simple que confirma la dirección y cierra las operaciones. |
| Long day from | 1 | Primer día de la ventana larga, como número, con el domingo en cero. Uno es lunes. |
| Long day to | 2 | Último día de la ventana larga. Dos es martes. |
| Short day from | 4 | Primer día de la ventana corta. Cuatro es jueves. |
| Short day to | 5 | Último día de la ventana corta. Cinco es viernes. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta la media móvil y dos conversores, uno para el precio de cierre y otro para el día de la semana de la hora de apertura.
- Cuatro comparaciones sitúan el día dentro o fuera de las dos ventanas del calendario, y otras dos colocan el cierre a un lado u otro de la media.
- Cada Y lógica une los dos extremos de una ventana, el lado de la media y la comprobación de posición plana antes de disparar un bloque de entrada.
- Los dos bloques de cierre cuelgan directamente de las comparaciones con la media y llevan la condición de cerrar posición, así que cada uno liquida solo su propio lado.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
