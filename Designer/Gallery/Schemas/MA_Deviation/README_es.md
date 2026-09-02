# Diagrama de la estrategia de desviación de la media móvil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La media móvil simple se toma como precio justo, y toda la señal es la distancia del cierre respecto a ella, medida en porcentaje. Cuando el precio se ha alejado demasiado de la media, el diagrama se posiciona en contra y devuelve la operación en cuanto el precio vuelve a tocar la media.

![schema](schema.svg)

## Resumen de la estrategia

- La desviación se calcula de forma literal, en un solo bloque de fórmula: (Close - SMA) / SMA * 100.
- Un único umbral sirve para ambos lados: la desviación se compara con ese número en positivo y en negativo, así el largo y el corto son simétricos.
- Solo se entra desde posición plana y ambos bloques de entrada llevan además la condición Abrir posición, por lo que nunca se promedia a la baja.
- El original trabaja con velas de un minuto, umbral del 2% y una pausa de 500 velas tras cada operación. El histórico incluido es de cinco minutos, así que el diagrama usa velas de cinco minutos con umbral del 1%, unas dos desviaciones típicas de esa serie; la pausa no se reproduce porque Designer no dispone de un contador de bloqueo, y por eso el diagrama opera más a menudo que el original.

## Reglas de entrada y salida

- **Entrada en largo**: La desviación está por debajo del umbral negativo, es decir, el cierre está más del porcentaje configurado por debajo de la media, y la posición es plana. La orden compra el volumen configurado.
- **Entrada en corto**: La desviación está por encima del umbral positivo, es decir, el cierre está más del porcentaje configurado por encima de la media, y la posición es plana. La orden vende el volumen configurado.
- **Salida**: El largo se cierra cuando el cierre vuelve a la media o por encima de ella; el corto, cuando el cierre vuelve a la media o por debajo. No hay stop loss ni take profit, igual que en la estrategia original.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SMA Length | 20 | Periodo de suavizado de la media móvil simple. |
| Deviation, % | 1 | Distancia respecto a la media, en porcentaje, que abre una operación. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta tanto el conversor que lee el precio de cierre como el bloque de indicador con la media móvil.
- Un bloque de fórmula convierte ese par en una desviación porcentual; una segunda fórmula mínima cambia el signo de la constante de umbral para que un solo parámetro cubra ambos lados.
- Dos bloques de comparación contrastan la desviación con los umbrales y otros dos comparan el cierre con la media para las salidas.
- El bloque de posición se compara con cero tres veces, lo que da los indicadores de plano, largo y corto que las Y lógicas unen a las condiciones de precio.
- Las entradas van a bloques de modificación de posición con la condición Abrir posición y una constante de volumen compartida; las salidas van a bloques con la condición Cerrar posición, que no necesitan volumen.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
