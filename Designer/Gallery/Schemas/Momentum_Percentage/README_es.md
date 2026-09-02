# Diagrama de la estrategia de cruce del cero del Momentum con filtro SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Aquí se apilan dos ideas. El Momentum, la diferencia entre el cierre actual y el cierre de diez velas atrás, dice hacia dónde ha empujado el mercado el precio en ese tramo, y el cambio de signo de esa diferencia es el disparador. Una media móvil simple hace de árbitro: el cruce solo se toma en la dirección con la que el cierre ya está de acuerdo.

![schema](schema.svg)

## Resumen de la estrategia

- El cruce de la línea cero se escribe con dos comparaciones, el valor actual contra cero y el valor de una vela atrás contra cero, que es exactamente la condición del código original.
- El filtro de la media móvil separa los dos cruces: el cruce al alza solo compra mientras el cierre está por encima de la media, el cruce a la baja solo vende mientras está por debajo.
- Pese al nombre de la carpeta, el indicador es Momentum, una diferencia absoluta de precios en puntos, y no una tasa de cambio porcentual.
- Cada señal invierte la posición: el volumen de la orden es el volumen compartido más el valor absoluto de la posición actual, así que una sola ejecución cierra el lado viejo y abre el nuevo.
- El original congela la operativa durante 30 velas tras cada ejecución; no existe un bloque contador de barras, así que esa pausa se omite y el diagrama responde a todos los cruces válidos.

## Reglas de entrada y salida

- **Entrada en largo**: El Momentum estaba en cero o por debajo en la vela anterior, ahora está por encima, el cierre está por encima de la SMA y la posición no es larga. La orden compra a mercado el volumen de vuelta.
- **Entrada en corto**: El Momentum estaba en cero o por encima en la vela anterior, ahora está por debajo, el cierre está por debajo de la SMA y la posición no es corta. La orden vende a mercado el volumen de vuelta.
- **Salida**: No hay bloque de salida propio ni stop de protección, igual que en el original: la posición se mantiene hasta que el cruce contrario la invierte con una sola orden.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Momentum Length | 10 | Número de velas que mira atrás el Momentum; el valor es el cierre actual menos el cierre de esas velas atrás. |
| SMA Length | 20 | Periodo de la media móvil simple que filtra la dirección del cruce. |
| Volume | 1 | Volumen base de la orden, en lotes; la orden de vuelta le suma el valor absoluto de la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta tres ramas: el indicador Momentum, la media móvil simple y un conversor que toma el precio de cierre.
- Un bloque de valor anterior guarda la lectura del Momentum de la vela pasada, y cuatro bloques de comparación sitúan la lectura actual y la anterior a uno u otro lado de una constante cero compartida.
- Otros dos bloques de comparación enfrentan el cierre a la media móvil, y dos más comparan la posición con esa misma constante cero.
- Cada Y lógica une el lado anterior del cero, el lado actual, el filtro de la media y el control de la posición, y dispara un bloque de modificación de posición cuyo volumen sale de una fórmula que suma el volumen y la posición absoluta.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
