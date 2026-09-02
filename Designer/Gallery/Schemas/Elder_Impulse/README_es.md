# Diagrama de la estrategia Elder Impulse System
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Alexander Elder colorea cada barra con dos cosas a la vez: la pendiente de una media móvil exponencial, que muestra la tendencia, y la pendiente del histograma del MACD, que muestra el impulso que la sostiene. Cuando ambas apuntan hacia arriba la barra es verde y el diagrama compra; cuando ambas apuntan hacia abajo la barra es roja y vende. Las órdenes se dimensionan como Volume más la posición abierta, de modo que cada señal da la vuelta a lo que se tenga.

![schema](schema.svg)

## Resumen de la estrategia

- La EMA y las líneas del MACD se calculan sobre velas cerradas de un solo instrumento; el histograma se construye dentro del diagrama como MACD menos Signal.
- Dos bloques de valor anterior guardan la EMA y el histograma de la vela previa, de manera que el diagrama compara la lectura actual con ella y deduce hacia dónde se inclina cada una.
- El color de la barra es el par de pendientes: EMA al alza e histograma al alza es verde; EMA a la baja e histograma plano o a la baja es rojo; cualquier otra combinación es neutra y se ignora.
- La estrategia original se aparta 65 barras tras cada operación. Esa pausa es un contador y los bloques del Designer no guardan ese estado, así que el diagrama la omite; de todos modos el control de la posición impide repetir el mismo lado.

## Reglas de entrada y salida

- **Entrada en largo**: La EMA está por encima de su valor de la vela anterior, el histograma también, y la posición no es ya larga. La orden compra Volume más la posición en valor absoluto: abre un largo desde plano o gira un corto de una sola vez.
- **Entrada en corto**: La EMA está por debajo de su valor de la vela anterior, el histograma está en ese valor o por debajo, y la posición no es ya corta. La orden vende Volume más la posición en valor absoluto: abre un corto desde plano o gira un largo.
- **Salida**: No hay salida propia: el color contrario invierte la posición y, como el tamaño de la orden incluye la posición abierta, el giro cierra la operación anterior y abre la nueva a la vez. La estrategia de origen tampoco lleva stop ni objetivo.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| EMA Length | 13 | Periodo de la media móvil exponencial cuya pendiente colorea la barra. |
| MACD Fast Length | 12 | Media móvil rápida del MACD. |
| MACD Slow Length | 26 | Media móvil lenta del MACD. |
| MACD Signal Length | 9 | Periodo de la línea de señal; el histograma es el MACD menos esa línea. |
| Volume | 1 | Volumen base de la orden, en lotes; al girar se le suma la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta dos bloques de indicador, EMA y MACD con línea de señal; dos convertidores extraen los valores MACD y Signal y un bloque de fórmula los resta para dar el histograma.
- Dos bloques de valor anterior, uno tipado como valor de indicador y otro como número, entregan las lecturas de la vela previa a cuatro bloques de comparación que resuelven las dos pendientes.
- Cada Y lógica une una condición de la EMA, una del histograma y una de la posición, de modo que solo se entra cuando la orden no aumenta el lado ya abierto.
- Un bloque de fórmula suma la posición absoluta a la constante de volumen compartida y alimenta ambos bloques de modificación de posición, que es lo que convierte cada señal en un giro.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
