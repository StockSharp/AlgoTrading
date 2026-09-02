# Diagrama de la estrategia de reversión del outside bar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un outside bar es una vela que se traga todo el rango de la anterior: un máximo más alto y un mínimo más bajo en la misma barra. Ambos bandos tuvieron su oportunidad dentro de una sola vela y uno de ellos ganó, así que el diagrama lee al ganador en el propio cuerpo de la barra: un outside bar alcista se compra y uno bajista se vende. Después, una media móvil simple del precio de cierre decide cuándo soltar la operación.

![schema](schema.svg)

## Resumen de la estrategia

- El outside bar se arma con bloques básicos: los conversores leen el máximo, el mínimo, la apertura y el cierre de la vela terminada, y dos bloques de valor anterior guardan el máximo y el mínimo de la vela previa.
- Dos comparaciones forman la figura —máximo por encima del máximo anterior y mínimo por debajo del mínimo anterior— y ambas deben cumplirse a la vez.
- La dirección sale del propio cuerpo de la vela, no de un filtro de tendencia: cerrar por encima de la apertura es comprar, cerrar por debajo es vender.
- La media móvil simple no interviene en la entrada y sirve únicamente como línea de salida, igual que en la estrategia original.

## Reglas de entrada y salida

- **Entrada en largo**: La vela ha superado los dos extremos de la anterior, cerró por encima de su propia apertura y no hay posición. La orden compra un lote y abre un largo.
- **Entrada en corto**: La vela ha superado los dos extremos de la anterior, cerró por debajo de su propia apertura y no hay posición. La orden vende un lote y abre un corto.
- **Salida**: El largo se cierra cuando una vela cierra por debajo de la media móvil y el corto cuando cierra por encima, ambos con bloques de modificación de posición en modo cierre, igual que en el original. No hay stop de pérdidas ni toma de beneficios porque el código original no los tiene. Queda fuera la pausa de varios cientos de velas que el original mantiene tras cada entrada y cada salida: un contador de barras solo se monta devolviendo una señal al diagrama, lo que cerraría el grafo en un bucle. Por eso aquí se actúa sobre cada outside bar y se opera bastante más a menudo.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SMA Length | 20 | Periodo de suavizado de la media móvil simple que cierra las operaciones. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. La estrategia original usa velas de un minuto; aquí se emplean cinco minutos para ajustarse al histórico incluido. |

## Detalles del diagrama

- El bloque de velas alimenta cinco ramas: cuatro conversores para apertura, máximo, mínimo y cierre, más la media móvil.
- El máximo y el mínimo salen por dos caminos a la vez —directamente a una comparación y a un bloque de valor anterior—, de modo que la comparación enfrenta el extremo de esta vela con el de la anterior.
- Cada Y lógica reúne cuatro señales: el máximo superior, el mínimo inferior, la dirección del cuerpo y el control de posición formado por el bloque de posición contra una constante cero.
- Los dos bloques de entrada envían órdenes a mercado y toman el volumen de una constante compartida; los dos bloques de salida se disparan directamente desde las comparaciones con la media y solo actúan cuando hay algo que cerrar.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
