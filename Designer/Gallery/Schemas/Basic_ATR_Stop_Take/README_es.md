# Diagrama de la estrategia de stop y objetivo por ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una lección breve sobre el riesgo medido en volatilidad. El cierre que cruza la EMA de 50 abre la operación, el cierre de esa misma vela se guarda como precio de entrada y, a partir de ahí, el diagrama mide cuánto se ha alejado el precio en unidades del rango verdadero medio. Un múltiplo del ATR cierra la operación con pérdida y otro la cierra con ganancia, de modo que la distancia de salida crece en mercados tranquilos y se estrecha en los agitados en vez de ser un número fijo de ticks.

![schema](schema.svg)

## Resumen de la estrategia

- Se usa un solo instrumento y una sola serie de velas: la EMA de 50 marca la dirección y el ATR de 14 aporta la vara de medir para las salidas.
- El precio de entrada lo sostienen dos bloques de variable: el primero toma el cierre de la vela que dio la señal y el segundo lo vuelve a emitir en cada vela siguiente para que las condiciones de salida se comprueben sin interrupción.
- Dos bloques de fórmula convierten la distancia al precio de entrada en múltiplos de ATR, uno a favor del largo y otro a favor del corto, de manera que los mismos dos umbrales sirven para ambos lados.
- La salida es una orden a mercado sobre vela cerrada, igual que en la estrategia de origen: no hay ningún stop en reposo en el mercado, así que un pico dentro de la vela no saca la operación.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre cruza la EMA al alza estando la posición plana. Se compra un lote y el cierre de esa vela pasa a ser el precio de entrada.
- **Entrada en corto**: El cierre cruza la EMA a la baja estando la posición plana. Se vende un lote y el cierre de esa vela pasa a ser el precio de entrada.
- **Salida**: La posición se cierra en la primera vela cerrada en la que el precio se ha movido StopFactor ATR en contra del precio de entrada o TakeFactor ATR a su favor. Ambos bloques de modificación están en modo cierre, así que cada uno actúa solo sobre su lado.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| EMA Length | 50 | Periodo de la media móvil exponencial que el cierre debe cruzar. |
| ATR Length | 14 | Periodo del rango verdadero medio que escala el stop y el objetivo. |
| Stop, ATR | 1.5 | Distancia del stop, en ATR: la pérdida que cierra la operación. |
| Take, ATR | 2 | Distancia del objetivo, en ATR: la ganancia que cierra la operación. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:15:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta un convertidor del precio de cierre, la EMA y el ATR; un bloque de cruce compara el cierre con la EMA y un NO lógico convierte el cruce a la baja en la señal corta.
- La posición actual se compara con una constante cero y cada Y lógica une esa comprobación con un cruce, de modo que solo se abre una operación desde plano.
- El precio de entrada lo guarda un par de bloques de variable; el segundo se dispara con la serie de velas, y por eso es el último enlace que sale del bloque de velas: así, ya en la vela de entrada la salida se mide contra el precio correcto.
- Cuatro bloques de comparación contrastan las dos distancias en ATR con las constantes de stop y objetivo, dos bloques O lógicos las unen y dos bloques de modificación en modo cierre envían las órdenes de salida.
- La estrategia de origen espera seis velas entre operaciones. Un contador así no tiene equivalente entre los bloques, por lo que el diagrama lo omite y toma el siguiente cruce de inmediato.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
