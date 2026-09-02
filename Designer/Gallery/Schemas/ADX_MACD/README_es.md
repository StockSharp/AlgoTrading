# Diagrama de la estrategia ADX + MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dos indicadores clásicos se reparten el trabajo: el MACD frente a su línea de señal indica hacia dónde se inclina el mercado y el ADX dice si el movimiento es lo bastante fuerte como para operarlo. Las entradas exigen ambos, mientras que la salida solo escucha al MACD, de modo que la posición se cierra en cuanto el impulso gira aunque la tendencia siga midiéndose como fuerte.

![schema](schema.svg)

## Resumen de la estrategia

- La línea ADX se extrae del valor compuesto del índice direccional medio y se compara con un único umbral de fuerza.
- La dirección viene del nivel de la línea MACD respecto a su señal, no del momento del cruce, así que se puede abrir una posición nueva en cualquier instante mientras el MACD permanezca de un lado.
- El filtro de fuerza solo protege las entradas: la salida se dispara únicamente por el paso del MACD al otro lado y el diagrama no lleva stop ni objetivo.

## Reglas de entrada y salida

- **Entrada en largo**: El ADX está por encima del umbral, la línea MACD está por encima de su señal y la posición está plana. El bloque de modificación compra a mercado el volumen compartido.
- **Entrada en corto**: El ADX está por encima del umbral, la línea MACD está por debajo de su señal y la posición está plana. El bloque de modificación vende a mercado el volumen compartido.
- **Salida**: El largo se cierra cuando la línea MACD cae por debajo de su señal y el corto cuando sube por encima; en la salida no se consulta el filtro ADX.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| ADX Length | 14 | Periodo del índice direccional medio, que fija tanto el índice direccional como su suavizado. |
| ADX Threshold | 25 | Nivel de fuerza que la línea ADX debe superar para permitir una entrada. |
| Fast EMA length | 12 | Periodo de la EMA rápida dentro del MACD. |
| Slow EMA length | 26 | Periodo de la EMA lenta dentro del MACD. |
| Signal EMA length | 9 | Periodo de la EMA de señal calculada sobre la línea MACD. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta ambos indicadores; los conversores extraen la línea ADX del índice direccional medio y las líneas MACD y señal del indicador MACD.
- Tres comparaciones producen las condiciones de mercado —fuerza de tendencia, MACD por encima de la señal y MACD por debajo— y otras tres comparan la posición con cero.
- Las Y lógicas de entrada unen fuerza, dirección y posición plana; las de salida unen dirección con una posición abierta del lado contrario.
- La pausa de 100 velas que la estrategia en C# mantiene entre operaciones no puede construirse con bloques de Designer, por lo que este diagrama entra y sale con más frecuencia.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
