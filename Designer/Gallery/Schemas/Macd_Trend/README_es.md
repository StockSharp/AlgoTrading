# Diagrama de la estrategia de tendencia MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama sigue la tendencia con el MACD: la diferencia entre una media móvil exponencial rápida y una lenta se suaviza otra vez para formar la línea de señal, y cada cruce entre ambas líneas da la vuelta a la posición. El volumen de la orden incluye la posición abierta, de modo que una sola orden cierra lo que se tiene y abre el lado contrario.

![schema](schema.svg)

## Resumen de la estrategia

- El MACD se construye en el diagrama a partir de sus piezas: EMA(12) menos EMA(26) es la línea MACD y una EMA(9) de esa línea es la de señal, con lo que los tres periodos siguen siendo parámetros del esquema.
- Un bloque de cruce compara las dos líneas y solo dispara en la vela en la que realmente se cruzan, hacia arriba o hacia abajo.
- Tras la primera señal la estrategia está siempre en mercado: no hay salida propia, el cruce contrario invierte la posición.

## Reglas de entrada y salida

- **Entrada en largo**: La línea MACD cruza por encima de la de señal y la posición todavía no es larga. La orden compra Volume más el valor absoluto de la posición actual: abre un largo desde plano o convierte un corto directamente en largo.
- **Entrada en corto**: La línea MACD cruza por debajo de la de señal y la posición todavía no es corta. La orden vende Volume más el valor absoluto de la posición actual: abre un corto desde plano o convierte un largo directamente en corto.
- **Salida**: No hay bloque de salida ni stop de protección: solo el cruce contrario saca de la posición, invirtiéndola con una única orden.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Fast EMA length | 12 | Periodo de la media móvil exponencial rápida dentro del MACD. |
| Slow EMA length | 26 | Periodo de la media móvil exponencial lenta dentro del MACD. |
| Signal EMA length | 9 | Periodo de suavizado de la línea de señal construida sobre la línea MACD. |
| Volume | 1 | Volumen base de la orden, en lotes; en una inversión se le suma la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta ambas medias y un bloque de fórmula resta la lenta a la rápida para obtener la línea MACD.
- La línea MACD continúa hacia un tercer bloque de indicador, una EMA(9), que es la línea de señal; ambas se encuentran en el bloque de cruce.
- La salida del cruce es la señal larga, un NO lógico sobre ella es la corta, y cada una se une mediante una Y lógica a la comparación de la posición con cero.
- Un segundo bloque de fórmula calcula Volume más la posición en valor absoluto y alimenta la entrada de volumen de los dos bloques de modificación de posición: así una orden a mercado invierte la posición.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
