# Diagrama de la estrategia de impulso con vela completa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una vela completa abre en un extremo de su rango y cierra en el otro: las sombras juntas ocupan como mucho una pequeña parte de la distancia entre máximo y mínimo. Esa barra es un único empuje ininterrumpido, y el diagrama se suma en el sentido del cuerpo siempre que una media móvil exponencial esté de acuerdo con esa dirección. A la operación se le da un objetivo fijo de una fracción de punto porcentual y nada más.

![schema](schema.svg)

## Resumen de la estrategia

- Los conversores leen la apertura, el máximo, el mínimo y el cierre de la vela terminada, y dos bloques de fórmula miden cuánto del rango ocupan las sombras.
- La medida alcista es la sombra superior más la inferior de una vela que sube, multiplicada por cien y comparada con la parte de sombra aplicada al rango completo; la medida bajista es su espejo.
- Una media móvil exponencial del precio de cierre actúa como filtro de tendencia: las velas completas alcistas solo se compran por encima y las bajistas solo se venden por debajo.
- Un bloque de protección de posición cierra cada operación con una toma de beneficios fija, la única salida que tiene la estrategia original.

## Reglas de entrada y salida

- **Entrada en largo**: La medida alcista de sombras está por debajo de cero, es decir, la vela subió y sus sombras se mantuvieron dentro de la parte permitida del rango; el cierre está por encima de la EMA y la posición no es ya larga. La orden compra la constante de volumen más el corto abierto, de modo que da la vuelta al corto y abre un largo en una sola orden.
- **Entrada en corto**: La medida bajista de sombras está por debajo de cero, el cierre está por debajo de la EMA y la posición no es ya corta. La orden vende la constante de volumen más el largo abierto, dando la vuelta al largo y abriendo un corto en una sola orden.
- **Salida**: El bloque de protección toma beneficios al 0,3 por ciento del precio de entrada, la misma cifra que la estrategia original lleva escrita en el código, y no hay stop de pérdidas porque el original tampoco lo tiene. Conviene conocer dos diferencias. El bloque de protección vigila el precio dentro de la barra, mientras que el original solo comprueba el cierre de una vela terminada, así que aquí el objetivo salta algo antes. Y la pausa de quince velas del original tras cada operación se ha dejado fuera: un contador de barras solo se monta devolviendo una señal al diagrama, lo que cerraría el grafo en un bucle, de modo que la señal de giro se toma en cuanto aparece.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| EMA Length | 20 | Periodo de la media móvil exponencial usada como filtro de tendencia. |
| Shadow share, % | 10 | Parte máxima del rango de la vela, en porcentaje, que pueden ocupar ambas sombras juntas. |
| Take profit, % | 0.3 | Distancia de la toma de beneficios respecto al precio de entrada, en porcentaje. |
| Volume | 1 | Volumen de la orden, en lotes; la orden de giro le suma el tamaño de la posición que se cierra. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. La estrategia original usa velas de quince minutos; aquí se emplean cinco minutos para que el patrón aparezca con suficiente frecuencia en el histórico incluido. |

## Detalles del diagrama

- Cada fórmula resta el presupuesto de sombra permitido de las sombras reales, así que un valor por debajo de cero significa vela de cuerpo lleno; la constante con la parte de sombra alimenta ambas fórmulas.
- La dirección no necesita comparación propia: escrita para una vela que sube, la medida alcista siempre es positiva en una que baja y en una sin rango, de modo que un valor bajo cero ya significa que la vela subió.
- El bloque de posición sale por dos caminos: hacia las comparaciones con cero que protegen las entradas y hacia la fórmula de volumen, que suma la posición en valor absoluto a la constante para que una sola orden a mercado cierre el lado contrario y abra el nuevo.
- Los dos bloques de entrada entregan sus operaciones al bloque de protección, que registra la toma de beneficios; el precio de cierre se envía a ese mismo bloque como referencia de precio.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
