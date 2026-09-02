# Diagrama de la estrategia de reversión en retrocesos de Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El rango de las últimas veinte velas se divide por la proporción áurea y los dos niveles de retroceso resultantes se usan como zonas de giro. Una vela que cierra sobre el nivel inferior con cuerpo alcista se compra, una que cierra sobre el nivel superior con cuerpo bajista se vende, y la SimpleMovingAverage decide cuándo termina la operación.

![schema](schema.svg)

## Resumen de la estrategia

- Highest y Lowest sobre la misma ventana dan el máximo y el mínimo del movimiento; su diferencia es el rango en el que se miden los niveles.
- El nivel de compra queda 0.618 del rango por debajo del máximo y el de venta 0.618 del rango por encima del mínimo; una vela está sobre un nivel mientras su cierre se sitúe a menos del dos por ciento del rango de él.
- Ambas distancias se calculan como fracción del rango, de modo que el diagrama funciona igual en cualquier instrumento y a cualquier escala de precios.
- Las entradas exigen además un cuerpo de vela que confirme y una posición plana; todas las salidas las decide la SimpleMovingAverage, porque la estrategia original no define stop ni objetivo.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre cae dentro del margen alrededor del nivel de retroceso inferior, la vela es alcista (cierre por encima de la apertura) y la posición está plana. El bloque compra un lote y abre un largo.
- **Entrada en corto**: El cierre cae dentro del margen alrededor del nivel de retroceso superior, la vela es bajista (cierre por debajo de la apertura) y la posición está plana. El bloque vende un lote y abre un corto.
- **Salida**: El largo se cierra en cuanto una vela cierra por debajo de la SimpleMovingAverage y el corto en cuanto una cierra por encima; ambos bloques trabajan en modo de cierre de posición, así que solo actúan si hay algo que cerrar.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Swing lookback | 20 | Número de velas sobre las que se toman el máximo y el mínimo del movimiento. |
| MA period | 20 | Periodo de la SimpleMovingAverage contra la que se miden las salidas. |
| Fibonacci ratio | 0.618 | Proporción de retroceso que sitúa ambos niveles dentro del rango. |
| Level buffer | 0.02 | Semiancho de la zona de entrada alrededor de un nivel, como fracción del rango. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- Un único bloque de velas alimenta Highest, Lowest y la SimpleMovingAverage, más dos conversores que extraen el cierre y la apertura de la vela.
- Dos bloques de fórmula convierten los precios en la distancia del cierre a cada nivel dividida por el rango, de manera que una sola constante de margen sirve para ambos lados.
- Cada entrada pasa por una Y lógica de tres señales: el nivel, el cuerpo de la vela y la posición comparada con una constante cero.
- Los dos bloques de salida se disparan directamente desde las comparaciones con la media móvil y están en modo de cierre; los cuatro bloques de órdenes comparten una misma constante de volumen.
- Simplificaciones deliberadas: el original trabaja con velas de un minuto y hace una pausa de 500 barras tras cada operación, algo que ningún bloque expresa, por lo que el diagrama usa velas de cinco minutos y vuelve a operar en cuanto se repiten las condiciones. Las posiciones duran unas pocas barras en lugar de días; subir el periodo de la media las alarga.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
