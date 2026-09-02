# Diagrama de la estrategia de stop dinámico por ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Las entradas son la parte sencilla: desde posición plana, un cierre por encima de la media móvil compra y un cierre por debajo vende. Lo interesante es la salida, un stop dinámico por ATR: una línea mantenida a varios rangos verdaderos medios del precio que acompaña al movimiento favorable y nunca cede terreno, cerrando la posición en cuanto el cierre la atraviesa.

![schema](schema.svg)

## Resumen de la estrategia

- Una media móvil simple de veinte periodos parte el gráfico en un lado alcista y otro bajista, y la posición del cierre respecto a ella decide la dirección de la entrada.
- El stop dinámico es un bloque SuperTrend: se trata exactamente de una banda de ATR con trinquete, así que la distancia del stop respira con la volatilidad en lugar de ser un número fijo de puntos.
- Toda entrada se toma solo desde posición plana y toda salida solo desde una posición del lado correspondiente, y eso es lo que impide que los cuatro bloques de orden se estorben.
- El nivel del stop es ancho a propósito —tres veces un ATR de catorce periodos— para que la posición sobreviva al ruido normal y se abandone solo cuando el movimiento gira de verdad.

## Reglas de entrada y salida

- **Entrada en largo**: La posición está plana y la vela cierra por encima de la media móvil simple. La orden compra el volumen compartido a mercado y la línea de ATR por debajo del precio pasa a ser el stop de ese largo.
- **Entrada en corto**: La posición está plana y la vela cierra por debajo de la media móvil simple. La orden vende el volumen compartido a mercado y la línea de ATR por encima del precio pasa a ser el stop de ese corto.
- **Salida**: El largo se cierra cuando el cierre cae por debajo de la línea dinámica de ATR y el corto cuando sube por encima de ella. No hay take-profit ni vuelta de posición: tras el stop el diagrama espera plano la siguiente señal de la media móvil.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| MA Period | 20 | Periodo de la media móvil simple que decide la dirección de la entrada. |
| ATR Period | 14 | Periodo del ATR dentro de la línea dinámica; valores mayores hacen que el stop reaccione más despacio a un cambio de volatilidad. |
| ATR Multiplier | 3 | Cuántos ATR separan la línea del precio; valores mayores dan más margen a la posición y provocan menos salidas. |
| Volume | 1 | Volumen de la orden, en lotes, compartido por los cuatro bloques de orden. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta la media móvil, la línea SuperTrend y un conversor que lee el precio de cierre.
- Dos comparaciones sitúan el cierre frente a la media móvil y otras dos frente a la línea dinámica, de modo que el mismo precio se lee una vez y lo usan las dos mitades del diagrama.
- Tres comparaciones contra una constante cero convierten la posición en indicadores de plano, largo y corto que habilitan por separado las entradas y las salidas.
- Los dos bloques de entrada llevan la condición de apertura y los dos de salida la de cierre, así que una señal que no encaja con la posición actual simplemente no hace nada.
- La estrategia original recalcula su nivel de stop como el máximo corriente del cierre menos varios ATR; ese trinquete no se puede expresar con una cadena de bloques, por lo que lo sustituye la línea SuperTrend, que funciona igual.
- Conviene conocer otras dos simplificaciones: la pausa de quinientas velas que el original mantiene tras cada operación no tiene bloque equivalente y se ha omitido, y el diagrama trabaja con velas de cinco minutos en lugar del minuto del código en C#, porque ese es el histórico que acompaña a la galería.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
