# Diagrama de la estrategia de ruptura del ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La mayoría de los diagramas compara un indicador con un nivel fijo. Este compara el índice direccional medio consigo mismo: una media móvil simple de la línea ADX es el centro, alrededor de ella se construye una banda con la distancia actual entre ambas, y romper esa banda se lee como un estallido repentino de fuerza de tendencia. La dirección la da la vela que lo produjo: si cierra por encima de su apertura se compra, en cualquier otro caso se vende.

![schema](schema.svg)

## Resumen de la estrategia

- La línea ADX del índice direccional medio es la única entrada de toda la construcción; las líneas +DI y -DI no se utilizan.
- Esa línea alimenta un segundo bloque de indicador, una media móvil simple de veinte periodos, de modo que el diagrama calcula un indicador sobre otro indicador.
- Un bloque de fórmula construye la banda como la media más el multiplicador por el doble de la distancia absoluta entre el ADX y su media, tal como lo calcula el código original.
- Las entradas giran una posición abierta con una sola orden, porque el volumen es el volumen compartido más lo que ya se tiene.

## Reglas de entrada y salida

- **Entrada en largo**: La línea ADX está por encima de la banda, la vela cerró por encima de su apertura y la posición no es larga. La orden compra el volumen compartido más el tamaño del corto abierto, así que una sola orden a mercado cierra el corto y abre el largo.
- **Entrada en corto**: La línea ADX está por encima de la banda, la vela cerró en su apertura o por debajo y la posición no es corta. La orden vende el volumen compartido más el tamaño del largo abierto.
- **Salida**: La posición se cierra en cuanto la línea ADX cae por debajo de su propia media móvil: el largo con una venta en modo cierre y el corto con una compra en modo cierre. Además, un bloque de protección de posición lleva el stop loss del dos por ciento del original; su take profit está puesto a cero, es decir desactivado, así que aquí tampoco hay objetivo. Conviene saber algo antes de optimizar: mientras el multiplicador se mantenga por debajo de 0.5, la condición de banda equivale algebraicamente a «ADX por encima de su media», de modo que con el valor por defecto 0.1 la banda no aporta nada y el diagrama se lee simplemente como el ADX cruzando su propia media hacia arriba y hacia abajo. El multiplicador se conserva como constante para que con valores mayores el comportamiento coincida con el original.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| ADX Length | 14 | Periodo de suavizado del índice direccional medio. |
| Average Length | 20 | Periodo de la media móvil simple que suaviza la línea ADX. |
| Multiplier | 0.1 | Multiplicador del ancho de banda; por debajo de 0.5 la banda se colapsa sobre la propia media móvil. |
| Stop Loss % | 2 | Distancia del stop loss respecto al precio de entrada, en porcentaje. |
| Volume | 1 | Volumen de la orden, en lotes, antes de sumarle la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el indicador ADX y dos conversores que leen la apertura y el cierre de la vela.
- Un conversor extrae la línea ADX del valor del indicador complejo y la entrega tanto al bloque de media móvil como a las comparaciones.
- Un único bloque de fórmula calcula toda la banda en una sola expresión, lo que mantiene la aritmética del original en un lugar legible en vez de repartirla en una cadena de bloques pequeños.
- Un segundo bloque de fórmula suma la posición absoluta al volumen compartido, y las dos salidas se disparan directamente desde la comparación «ADX por debajo de su media», así que solo actúan cuando hay algo que cerrar.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
