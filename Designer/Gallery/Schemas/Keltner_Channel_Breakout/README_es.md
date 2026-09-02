# Diagrama de la estrategia de ruptura del canal de Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un canal de Keltner es una media móvil exponencial con bordes separados por un múltiplo del rango verdadero medio. El diagrama espera un cierre fuera de un borde dentro del cual el cierre anterior todavía estaba, y gira toda la posición en el sentido de la ruptura. No hay stop ni objetivo: la ruptura contraria es lo que retira la operación.

![schema](schema.svg)

## Resumen de la estrategia

- KeltnerChannels produce el canal en un solo bloque y dos conversores extraen de su valor el borde superior y el inferior.
- Los bloques de valor anterior guardan los dos bordes y el cierre de una barra atrás, de modo que la ruptura se mide contra el nivel que el mercado ya vio y no contra un borde que se movió con la misma vela.
- Cada orden lleva el volumen compartido más el valor absoluto de la posición, así que una sola orden da la vuelta a la operación en lugar de reducirla.
- El original en C# usa un canal de periodo 500 con multiplicador 10 en velas de un minuto; el diagrama emplea el canal 20 / 2 documentado en su README sobre velas de cinco minutos, para que la ruptura ocurra de verdad con datos corrientes.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre está por encima de la banda superior de la vela anterior mientras el cierre anterior seguía en ella o por debajo, y la posición no es larga. La orden compra el volumen más el corto abierto, con lo que se pasa a largo.
- **Entrada en corto**: El cierre está por debajo de la banda inferior de la vela anterior mientras el cierre anterior seguía en ella o por encima, y la posición no es corta. La orden vende el volumen más el largo abierto, con lo que se pasa a corto.
- **Salida**: No hay bloque de salida: la ruptura contraria invierte la posición, exactamente como en la estrategia original, que no tiene stop ni objetivo.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Channel period | 20 | Periodo del canal de Keltner; fija tanto la media móvil como el rango del que se calcula la anchura. |
| ATR multiplier | 2 | Cuántos rangos separan los bordes del canal de la línea central. |
| Volume | 1 | Volumen de la orden, en lotes, antes de sumarle la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el indicador y un conversor que lee el precio de cierre.
- Tres bloques de valor anterior desplazan la banda superior, la inferior y el cierre una barra; el indicador solo emite cuando está formado, así que las primeras barras se saltan solas.
- Cuatro bloques de comparación forman cada lado de la ruptura: uno para la vela que sale y otro para la que aún estaba dentro.
- La posición se compara con una constante cero y entra en ambas Y lógicas, mientras un bloque de fórmula suma su valor absoluto a la constante de volumen para dimensionar la orden de vuelta.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
