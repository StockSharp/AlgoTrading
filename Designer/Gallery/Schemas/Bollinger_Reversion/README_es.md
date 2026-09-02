# Diagrama de la estrategia de reversión de Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un cierre fuera de una banda de Bollinger se interpreta como un estiramiento a punto de devolverse: el diagrama compra por debajo de la banda inferior, vende por encima de la superior y mantiene la posición solo hasta que el precio vuelve a tocar la línea media. A diferencia de un diagrama de ruptura sobre las mismas bandas, aquí se entra contra el movimiento y el objetivo es la línea media, no la banda contraria.

![schema](schema.svg)

## Resumen de la estrategia

- BollingerBands se calcula una vez y se lee tres veces: banda superior, banda inferior y la media móvil central.
- Solo se entra desde posición plana, de modo que una serie de cierres fuera de la banda no añade nada a una posición ya abierta.
- La salida es simétrica a la entrada: la línea media es el objetivo y el bloque de cierre envía exactamente el tamaño de la posición abierta.
- El ancho de las bandas y su periodo están expuestos, así que el mismo diagrama sirve para un instrumento tranquilo y para uno volátil.

## Reglas de entrada y salida

- **Entrada en largo**: La vela cierra por debajo de la banda inferior y la posición es plana. La orden compra el volumen base y abre un largo contra el movimiento.
- **Entrada en corto**: La vela cierra por encima de la banda superior y la posición es plana. La orden vende el volumen base y abre un corto contra el movimiento.
- **Salida**: El largo se cierra en el primer cierre en la línea media o por encima; el corto, en el primer cierre en la línea media o por debajo. La estrategia original no tiene stop ni take profit; su pausa de quinientas velas y su límite de trescientas velas por posición no se trasladan y, como la pausa era más larga que el límite, en el código fuente cada operación terminaba en realidad por tiempo y la salida a la línea media nunca llegaba a ejecutarse.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Bollinger Period | 20 | Periodo de suavizado de las bandas de Bollinger. |
| Bollinger Width | 2 | Ancho de las bandas en desviaciones estándar. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas: la estrategia original usaba velas de un minuto y el diagrama trabaja con velas de cinco minutos. |

## Detalles del diagrama

- El bloque de velas alimenta el indicador y un convertidor del precio de cierre; otros tres convertidores extraen las bandas y la línea media del valor del indicador.
- Cuatro bloques de comparación convierten el cierre en señales: fuera de la banda inferior, fuera de la superior, de vuelta a la media desde abajo y de vuelta a la media desde arriba.
- El bloque de posición alimenta tres comparaciones con cero que protegen las dos entradas y las dos salidas.
- Los bloques de entrada usan la condición de apertura y comparten una constante de volumen; los de salida usan la condición de cierre y toman el volumen de la propia posición.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
