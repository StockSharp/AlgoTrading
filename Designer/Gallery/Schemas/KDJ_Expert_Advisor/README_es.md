# Diagrama de la estrategia del asesor KDJ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una adaptación del asesor KDJ de MetaTrader. La línea J se reconstruye aquí como la diferencia entre las líneas %K y %D del oscilador estocástico, y esa diferencia decide el lado: se compra cuando pasa a positiva o cuando %K sigue subiendo con la diferencia ya positiva, y se vende en las condiciones simétricas. Dos cosas se adaptan al histórico incluido: las velas de cuatro horas del original pasan a ser de una hora, para que un mes de datos siga dando suficientes barras, y el stop y el objetivo en pips se convierten en distancias porcentuales válidas para cualquier instrumento.

![schema](schema.svg)

## Resumen de la estrategia

- El oscilador estocástico con %K de 30 barras y %D de 6 hace las veces de KDJ, y la diferencia K - D cumple el papel de la línea J.
- Hay dos formas de entrar: que la diferencia cruce el cero, o que la línea %K se mueva en la dirección que el signo de la diferencia ya señala.
- La posición solo se abre desde plano, así que la estrategia nunca piramida ni se da la vuelta; quien cierra la operación es el bloque de protección.

## Reglas de entrada y salida

- **Entrada en largo**: K - D es positiva y, o bien era negativa en la vela anterior (esta vela es el cruce del cero), o bien %K es mayor que en la vela anterior. La posición debe estar plana; se compra un lote a mercado.
- **Entrada en corto**: K - D es negativa y, o bien era positiva en la vela anterior (esta vela es el cruce del cero), o bien %K es menor que en la vela anterior. La posición debe estar plana; se vende un lote a mercado.
- **Salida**: No hay ninguna señal de salida, igual que en el original: el bloque de protección cierra la operación con órdenes a mercado en un objetivo del 2% o un stop del 1%, el equivalente porcentual de las distancias de 450 y 250 pips del código.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| %K Length (KDJ period) | 30 | Longitud de la línea %K, el periodo KDJ del asesor original. |
| %D Smoothing | 6 | Longitud de suavizado de la línea %D. |
| Take profit, % | 2 | Distancia del objetivo, en porcentaje del precio de entrada. |
| Stop loss, % | 1 | Distancia del stop, en porcentaje del precio de entrada. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 01:00:00 | Marco temporal de las velas de todo el diagrama; el original usaba cuatro horas. |

## Detalles del diagrama

- Dos bloques convertidores separan el estocástico en sus líneas %K y %D, y un bloque de fórmula resta una de la otra.
- Los bloques de valor anterior guardan K - D y %K una vela atrás, que es como se reconocen el cruce del cero y la pendiente sin usar un bloque de cruce.
- Cuatro Y lógicas construyen las dos vías de entrada de cada dirección y ya incluyen la señal de posición plana; una O une el par en un único disparo por lado.
- Ambos bloques de entrada envían sus propias operaciones al bloque de protección, de modo que cada ejecución recibe enseguida un stop y un objetivo.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
