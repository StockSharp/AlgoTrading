# Diagrama de la estrategia de impulso Momentum en la línea cero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Todo el diagrama descansa sobre un solo número: la diferencia entre el cierre actual y el cierre de doce velas atrás. Mientras esa diferencia es positiva el mercado ha empujado el precio hacia arriba durante la ventana; mientras es negativa lo ha empujado hacia abajo, y en cuanto cambia de signo el diagrama da la vuelta a la posición. Pese al nombre de la carpeta, el original usa Momentum, una diferencia absoluta de precios, y no una tasa de cambio porcentual.

![schema](schema.svg)

## Resumen de la estrategia

- El Momentum de 12 velas se compara con la línea cero, y el valor anterior del mismo indicador dice de qué lado venía, de modo que dos comparaciones forman un cruce completo.
- Cada señal es una vuelta: el volumen de la orden es el volumen compartido más el valor absoluto de la posición actual, así que una sola orden cierra el lado viejo y abre el nuevo.
- La posición interviene en ambas ramas: el cruce al alza solo se compra si aún no se está largo y el cruce a la baja solo se vende si aún no se está corto.
- El original congela además la operativa durante 55 velas tras cada ejecución; no existe un bloque contador de barras, así que esa pausa se omite y el diagrama responde a todos los cruces.

## Reglas de entrada y salida

- **Entrada en largo**: En la vela anterior el Momentum estaba en cero o por debajo, ahora está por encima y la posición no es larga. La orden compra el volumen de vuelta a mercado, cerrando cualquier corto y abriendo el largo en un solo paso.
- **Entrada en corto**: En la vela anterior el Momentum estaba en cero o por encima, ahora está por debajo y la posición no es corta. La orden vende el volumen de vuelta a mercado, cerrando cualquier largo y abriendo el corto en un solo paso.
- **Salida**: No hay bloque de salida propio. La posición se mantiene hasta que el cruce contrario de la línea cero la invierte, y el original no tiene stop de pérdidas ni el stop por ATR que menciona su README.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Momentum Length | 12 | Número de velas que mira atrás el Momentum: el valor es el cierre actual menos el cierre de esas velas atrás. |
| Volume | 1 | Volumen base de la orden, en lotes; la orden de vuelta le suma el valor absoluto de la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el indicador Momentum, cuya salida va tanto a los bloques de comparación como a un bloque de valor anterior que guarda la lectura de la vela pasada.
- Cuatro bloques de comparación comparten una constante cero, que sirve además de referencia para las dos comprobaciones de posición.
- Cada Y lógica une el lado actual del cero, el lado anterior y la condición de posición, y dispara un bloque de modificación de posición.
- Un bloque de fórmula calcula el tamaño de vuelta como el volumen compartido más la posición absoluta y alimenta el volumen de ambas órdenes.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
