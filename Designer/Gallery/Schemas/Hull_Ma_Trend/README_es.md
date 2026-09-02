# Diagrama de la estrategia de pendiente de la Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Hull Moving Average sigue al precio con muy poco retraso, así que la dirección de su propia pendiente ya es una señal de tendencia. El diagrama mide cuánto se movió la media desde la vela anterior, como fracción de su propio valor, y gira la posición hacia ese lado en cuanto el movimiento supera un umbral pequeño. El original cuenta 500 velas de un minuto; aquí la longitud es de 100 velas de cinco minutos, el mismo tramo de tiempo sobre el histórico incluido.

![schema](schema.svg)

## Resumen de la estrategia

- Solo se opera la pendiente de la Hull Moving Average: el precio nunca se compara con la media.
- La pendiente es relativa, expresada como fracción del valor anterior, de modo que el mismo umbral sirve a cualquier nivel de precio.
- Por encima de +0,02% el diagrama quiere estar largo, por debajo de -0,02% corto; dentro de esa banda no ocurre nada y se mantiene la posición abierta.
- Tras la primera señal la estrategia está siempre en el mercado: no hay stop, ni objetivo, ni estado plano entre operaciones, igual que en el código original.

## Reglas de entrada y salida

- **Entrada en largo**: La Hull Moving Average subió más que el umbral de subida desde la vela anterior y la posición no es larga. La orden compra el volumen compartido más el tamaño del corto abierto, así que una sola orden gira la posición.
- **Entrada en corto**: La Hull Moving Average bajó más que el umbral de bajada desde la vela anterior y la posición no es corta. La orden vende el volumen compartido más el tamaño del largo abierto.
- **Salida**: No hay bloque de salida: la señal de pendiente contraria gira la posición y, como el volumen de la orden ya contiene la posición absoluta, una única orden a mercado cierra un lado y abre el otro.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Hull MA Length | 100 | Longitud de la Hull Moving Average, reescalada de 500 velas de un minuto a 100 de cinco minutos. |
| Rise Threshold | 0.0002 | Subida relativa de la media en una vela que abre un largo; 0,0002 es 0,02%. |
| Fall Threshold | -0.0002 | Bajada relativa de la media en una vela que abre un corto; el reflejo del umbral de subida. |
| Volume | 1 | Volumen de la orden, en lotes, antes de sumarle la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- Un bloque de valor anterior guarda la Hull de la vela previa y calla en el primer valor, lo que reproduce la barra inicial que el original descarta.
- La fórmula de la pendiente resta el valor anterior al actual y divide por el anterior, convirtiendo el movimiento en una fracción.
- Dos comparaciones parten esa fracción en tres estados con las constantes de umbral positiva y negativa.
- Cada Y lógica une una condición de pendiente con una comprobación de posición, y la fórmula de volumen suma la posición absoluta al volumen compartido, que es lo que convierte una entrada en un giro.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
