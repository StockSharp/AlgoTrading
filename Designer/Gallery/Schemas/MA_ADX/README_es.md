# Diagrama de la estrategia MA + ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un diagrama de tendencia con filtro de fuerza. La ExponentialMovingAverage indica en qué lado del mercado hay que estar, el índice direccional DX decide si el movimiento merece una posición y esta se abandona en cuanto el cierre vuelve al otro lado de la media.

![schema](schema.svg)

## Resumen de la estrategia

- El cierre de la vela se compara con una ExponentialMovingAverage: por encima significa largo y por debajo, corto.
- El bloque DirectionalIndex entrega el valor DX, la misma fórmula que la estrategia original calcula a mano a partir de +DM y -DM; solo se permite entrar mientras DX supera el umbral.
- Las entradas se hacen únicamente desde posición plana y cada salida cierra exactamente lo abierto, de modo que nunca se piramida.
- La salida no mira la fuerza de la tendencia: en cuanto el cierre queda al otro lado de la media, la posición se cierra sin importar el DX.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre está por encima de la EMA, el DX supera el umbral de fuerza de tendencia y la posición es plana. La orden compra el volumen base y abre un largo.
- **Entrada en corto**: El cierre está por debajo de la EMA, el DX supera el umbral de fuerza de tendencia y la posición es plana. La orden vende el volumen base y abre un corto.
- **Salida**: El largo se cierra en cuanto una vela cierra por debajo de la EMA y el corto en cuanto cierra por encima; los bloques de cierre toman el volumen de la posición abierta. La estrategia original no tiene stop ni take profit, y su pausa de cien velas tras cada operación no se reproduce, por lo que este diagrama opera con más frecuencia que el código fuente.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| EMA Length | 20 | Periodo de la media exponencial que marca la dirección. |
| DX Length | 14 | Periodo del índice direccional que mide la fuerza de la tendencia. |
| Trend Strength | 25 | Valor de DX por encima del cual se permite una nueva posición. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta los dos indicadores y un convertidor que extrae el precio de cierre.
- Dos bloques de comparación sitúan el cierre respecto a la EMA y se reutilizan: la misma señal abre un lado y cierra el otro.
- El bloque de posición alimenta tres comparaciones con cero: la posición plana protege las entradas, y largo y corto protegen las dos salidas.
- Los bloques de entrada usan la condición de apertura y toman el volumen de una constante compartida; los de salida usan la condición de cierre y calculan el volumen por sí mismos.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
