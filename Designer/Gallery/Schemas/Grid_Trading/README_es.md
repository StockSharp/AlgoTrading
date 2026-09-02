# Diagrama de la estrategia de trading en rejilla
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama convierte el precio en una escalera: el cierre de cada vela se redondea hacia abajo a un múltiplo del paso de rejilla y solo el salto a un escalón nuevo cuenta como señal. Subir un escalón compra, bajarlo vende, de modo que la posición sigue siempre el sentido en que se cruzó la rejilla.

![schema](schema.svg)

## Resumen de la estrategia

- El precio de cierre se discretiza con la fórmula floor(Close / GridStep) * GridStep, que da el escalón en el que se encuentra el mercado.
- Un bloque de valor anterior guarda el escalón de la vela previa, así se comparan escalones y no precios, y todo movimiento dentro de una celda de la rejilla se ignora.
- El volumen de la orden es la posición abierta más el volumen base, por lo que una señal contraria a la posición la invierte con una sola orden a mercado.
- La estrategia original trabaja con velas de cuatro horas y cierra con un beneficio absoluto de 2000 unidades de precio; aquí se usan velas de cinco minutos y el objetivo se expresa como porcentaje del precio de entrada, lo que lo hace válido en cualquier instrumento.

## Reglas de entrada y salida

- **Entrada en largo**: El nuevo escalón de la rejilla está por encima del anterior y la posición no es larga. La orden compra el volumen base más el corto abierto, y la posición queda larga por un volumen base.
- **Entrada en corto**: El nuevo escalón de la rejilla está por debajo del anterior y la posición no es corta. La orden vende el volumen base más el largo abierto, y la posición queda corta por un volumen base.
- **Salida**: El bloque de protección cierra la posición con un take profit del porcentaje configurado; no hay stop loss, igual que en el original. En los demás casos la posición se mantiene hasta que el precio pasa a la siguiente celda y la señal contraria la invierte.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Grid Step | 500 | Altura de un escalón de la rejilla, en unidades de precio del instrumento. |
| Take Profit, % | 3 | Take profit, como porcentaje del precio medio de entrada. |
| Volume | 1 | Volumen base de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta un conversor que lee el precio de cierre, y un bloque de fórmula lo redondea hacia abajo a la rejilla.
- Un bloque de valor anterior retrasa el escalón una vela; dos bloques de comparación deciden si el escalón subió o bajó.
- Dos comparaciones de la posición con cero se unen a las señales de la rejilla mediante Y lógicas, de modo que un cambio de escalón nunca aumenta una posición ya abierta en ese sentido.
- Una segunda fórmula calcula |Position| + Volume y alimenta la entrada de volumen de ambos bloques de modificación de posición: por eso la inversión se hace con una sola orden.
- Las operaciones propias de ambos bloques van al bloque de protección de posición, cuya entrada de precio es el cierre de las velas terminadas.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
