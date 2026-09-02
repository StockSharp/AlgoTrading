# Diagrama de la estrategia de cruce %K/%D del Stochastic en zonas extremas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El cruce de las dos líneas del Stochastic es una señal frecuente y ruidosa, así que este diagrama solo la acepta donde significa algo: el cruce alcista debe producirse mientras %K sigue en la zona de sobreventa y el bajista mientras %K sigue sobrecomprado. Cada señal aceptada invierte la posición, de modo que el diagrama está siempre largo o corto y nunca solo esperando.

![schema](schema.svg)

## Resumen de la estrategia

- Un único bloque del Stochastic Oscillator aporta las dos líneas; los bloques conversores separan su valor en %K y %D.
- Un bloque de cruce compara ambas líneas: su señal marca el cruce alcista y esa misma señal invertida por un bloque NO marca el bajista.
- El filtro de zona es una simple comparación de %K con las constantes de sobreventa y sobrecompra, así que un cruce en mitad del rango se ignora.
- El volumen de la orden es el volumen base más el valor absoluto de la posición, lo que cierra el lado contrario y abre el nuevo con una sola orden a mercado.
- Pese al nombre de la carpeta de la estrategia original, en ella no hay RSI ni stop loss; la pausa de cinco velas que mantiene tras cada operación no tiene equivalente en bloques y se omite.
- El original trabaja con velas de quince minutos; el diagrama se ha reducido a velas de cinco minutos para ajustarse al histórico de muestra incluido.

## Reglas de entrada y salida

- **Entrada en largo**: %K cruza por encima de %D mientras %K está bajo el nivel de sobreventa y la posición no es ya larga. La orden compra el volumen base más el corto abierto, invirtiendo la posición a largo.
- **Entrada en corto**: %K cruza por debajo de %D mientras %K está sobre el nivel de sobrecompra y la posición no es ya corta. La orden vende el volumen base más el largo abierto, invirtiendo la posición a corto.
- **Salida**: No hay bloque de salida propio: la posición se mantiene hasta que aparece el cruce contrario en la zona opuesta, y esa orden cierra la operación antigua y abre la nueva.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| %K Length | 14 | Periodo de cálculo de la línea %K del Stochastic. |
| %D Length | 3 | Periodo de suavizado de la línea %D, la media móvil de %K. |
| Oversold | 20 | Nivel por debajo del cual se acepta un cruce alcista como compra. |
| Overbought | 80 | Nivel por encima del cual se acepta un cruce bajista como venta. |
| Volume | 1 | Volumen base de la orden, en lotes; la inversión le suma la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta un solo Stochastic Oscillator y dos conversores extraen de su valor las líneas %K y %D.
- El bloque de cruce se dispara únicamente en la vela en la que las líneas intercambian posiciones, que es lo que evita operar en cada barra en la que están separadas.
- Cada Y lógica une el cruce, la comparación de zona y una comprobación de posición antes de disparar un bloque de modificación de posición.
- Un bloque de fórmula suma el volumen base al valor absoluto de la posición y alimenta ambos bloques de orden, de forma que una sola orden a mercado ejecuta toda la inversión.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
