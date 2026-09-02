# Diagrama de la estrategia Opening Range Breakout (ruptura de Bandas de Bollinger con filtro EMA)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El ejemplo conserva el nombre de la estrategia original, pero no contiene ningún rango de apertura de sesión: lo que realmente opera es una ruptura de las Bandas de Bollinger confirmada por una EMA lenta. La salida del precio fuera de la banda es el disparador, la EMA decide si esa ruptura va a favor o en contra del mercado y la banda central devuelve la operación a casa.

![schema](schema.svg)

## Resumen de la estrategia

- Las Bandas de Bollinger y una EMA de 50 periodos se calculan sobre las mismas velas de media hora y toda decisión usa el cierre de una vela terminada.
- Una ruptura solo cuenta en el sentido de la tendencia: por encima de la banda superior el cierre debe estar además sobre la EMA, y por debajo de la inferior debe estar además bajo ella.
- La banda central sirve de salida para ambos lados, de modo que la operación dura exactamente lo que el precio se mantenga lejos de su propia media. No hay stop ni objetivo de beneficio.

## Reglas de entrada y salida

- **Entrada en largo**: La vela cierra por encima de la banda superior de Bollinger, ese mismo cierre está sobre la EMA y la posición está plana. El bloque de modificación compra a mercado el volumen compartido.
- **Entrada en corto**: La vela cierra por debajo de la banda inferior de Bollinger, ese mismo cierre está bajo la EMA y la posición está plana. El bloque de modificación vende a mercado el volumen compartido.
- **Salida**: El primer cierre por debajo de la banda central cierra el largo y el primero por encima cierra el corto; ambos bloques trabajan en modo de cierre, así que solo actúan si hay algo que cerrar.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Bollinger Length | 20 | Periodo de suavizado de las Bandas de Bollinger, que es también el de la banda central. |
| Bollinger Width | 2 | Anchura de las bandas en desviaciones típicas; el código original la fija en dos. |
| EMA Length | 50 | Periodo de la EMA que decide en qué dirección se permite operar la ruptura. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:30:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta las Bandas de Bollinger, la EMA y un conversor del precio de cierre; otros tres conversores separan la banda superior, la inferior y la central.
- Seis comparaciones cubren toda la lógica: dos para las bandas, dos para el filtro de la EMA y dos para el regreso a la banda central.
- Las dos Y lógicas de entrada exigen posición plana, así que una entrada nunca añade a una operación abierta; los bloques de cierre cuelgan directamente de las comparaciones con la banda central.
- Faltan dos cosas del original en C#: la pausa de 10 velas entre acciones, que no tiene bloque en Designer, y la vuelta inmediata de posición — aquí primero se cierra en la banda central y el lado contrario se abre en una vela posterior.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
