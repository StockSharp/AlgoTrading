# Diagrama de la estrategia Dark Cloud Cover / Piercing Line con CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dos patrones clásicos de reversión de dos velas eligen el lado y el Commodity Channel Index decide si la reversión merece la pena. Una Piercing Line solo se compra mientras el CCI está hundido en terreno negativo; un Dark Cloud Cover solo se vende mientras el CCI está estirado al alza. Ninguna señal cierra la operación: de eso se encargan el take profit y el stop loss colocados en la entrada.

![schema](schema.svg)

## Resumen de la estrategia

- Dos bloques de indicador de patrones de velas llevan expresiones escritas a mano que describen la figura: la dirección de la vela anterior, la de la actual, dónde abrió y si cerró más allá del centro del cuerpo anterior.
- El Commodity Channel Index de catorce velas actúa como confirmación: el mercado ya debe estar estirado en la dirección que el patrón revierte, de lo contrario la figura se ignora.
- Una única constante de nivel de entrada sirve a ambos lados, porque una fórmula le cambia el signo para la comparación larga.
- Solo se entra estando plano, de modo que un patrón que se repite en la vela siguiente no duplica la operación.

## Reglas de entrada y salida

- **Entrada en largo**: La vela anterior es bajista, la actual es alcista, abrió por debajo del cierre anterior y cerró por encima del centro del cuerpo anterior, el CCI está por debajo del nivel de entrada en negativo y la posición está plana. La orden compra un lote a mercado.
- **Entrada en corto**: La vela anterior es alcista, la actual es bajista, abrió por encima del cierre anterior y cerró por debajo del centro del cuerpo anterior, el CCI está por encima del nivel de entrada y la posición está plana. La orden vende un lote a mercado.
- **Salida**: Solo el bloque de protección de la posición: un take profit al dos por ciento del precio de entrada y un stop loss al uno por ciento. La estrategia original tampoco tiene salida por señal, así que aquí no falta nada.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| CCI Length | 14 | Periodo de suavizado del Commodity Channel Index. |
| Entry Level | 50 | Cuánto debe alejarse el CCI de cero para dar por confirmado un patrón; el lado largo usa este número en negativo. |
| Take Profit % | 2 | Distancia del take profit respecto al precio de entrada, en porcentaje. |
| Stop Loss % | 1 | Distancia del stop loss respecto al precio de entrada, en porcentaje. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta los dos bloques de patrones, el Commodity Channel Index y el conversor que entrega el precio de cierre al bloque de protección.
- Una constante guarda el nivel de entrada y una fórmula le invierte el signo, por lo que un único número optimizable gobierna las dos comparaciones del CCI.
- Cada Y lógica une un patrón, su confirmación por CCI y la comprobación de posición plana, y dispara un bloque de modificación de posición en modo de solo apertura.
- Se han simplificado dos cosas del original: allí también se exige un hueco real más allá del mínimo o del máximo de la vela previa, algo que un instrumento de cotización continua casi nunca muestra, y una pausa de seis velas entre operaciones, para la que no existe bloque contador. Por eso aquí solo se pide que la apertura quede al otro lado del cierre anterior y se opera cada patrón confirmado.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
