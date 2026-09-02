# Diagrama de la estrategia MACD + RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El MACD marca la dirección y el RSI marca el momento. Mientras la línea MACD está por encima de su línea de señal, el diagrama espera a que el índice de fuerza relativa caiga en la zona de sobreventa y compra ese retroceso; la regla espejo vende un RSI sobrecomprado mientras el MACD está por debajo de su señal. La posición se devuelve en cuanto las dos líneas del MACD intercambian sus lugares.

![schema](schema.svg)

## Resumen de la estrategia

- La prueba de tendencia es una comparación de niveles, no un cruce: lo que cuenta es en qué lado de la línea de señal se encuentra el MACD, de modo que el filtro sigue activo mientras dure la tendencia.
- La entrada dentro de esa tendencia es deliberadamente contraria: el RSI tiene que estar estirado en contra, así que el diagrama compra retrocesos en vez de perseguir rupturas.
- La salida usa el mismo par de líneas: el largo se cierra cuando el MACD cae por debajo de su señal y el corto cuando sube por encima.
- No hay stop de pérdidas ni toma de beneficios, igual que en la estrategia original, donde el giro del MACD es la única salida.

## Reglas de entrada y salida

- **Entrada en largo**: La línea MACD está por encima de su señal, el RSI por debajo del nivel de sobreventa y la posición está plana. La orden compra un lote a mercado.
- **Entrada en corto**: La línea MACD está por debajo de su señal, el RSI por encima del nivel de sobrecompra y la posición está plana. La orden vende un lote a mercado.
- **Salida**: El largo se cierra en la primera vela en la que el MACD cae por debajo de su señal y el corto en la primera en la que sube por encima; los dos bloques de cierre leen el volumen de la posición abierta.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| MACD Fast Length | 12 | Periodo de la EMA rápida dentro del MACD. |
| MACD Slow Length | 26 | Periodo de la EMA lenta dentro del MACD. |
| MACD Signal Length | 9 | Periodo de la EMA que suaviza el MACD hasta formar la línea de señal. |
| RSI Length | 14 | Periodo de suavizado del índice de fuerza relativa. |
| RSI Oversold | 30 | Nivel por debajo del cual el RSI se considera sobrevendido y se permite comprar. |
| RSI Overbought | 70 | Nivel por encima del cual el RSI se considera sobrecomprado y se permite vender. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- Un bloque de indicador contiene el MACD con su línea de señal; dos convertidores extraen los valores Macd y Signal, y otro bloque de indicador calcula el índice de fuerza relativa sobre las mismas velas.
- Dos comparaciones sitúan la línea MACD frente a la de señal, otras dos sitúan el RSI frente a las constantes de umbral y una compara la posición con cero.
- Cada Y lógica une una condición de tendencia, una de RSI y la comprobación de posición plana, y después dispara un bloque de modificación que solo abre desde plano.
- Las comparaciones de tendencia se reutilizan como disparadores de salida, así que los dos bloques de cierre no necesitan lógica adicional. La pausa de 150 barras entre operaciones del original no tiene equivalente entre los bloques y se omite, por lo que las reentradas son más frecuentes que en el código.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
