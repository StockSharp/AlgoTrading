# Diagrama de la estrategia Bandas de Bollinger + Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una reversión a la media que exige dos señales independientes de movimiento agotado: el cierre debe alcanzar una banda de Bollinger y la línea %K del Stochastic debe estar en la zona extrema correspondiente. La posición se devuelve en cuanto el precio cruza la banda central de esas mismas bandas, de modo que la operación dura exactamente lo que dura la desviación.

![schema](schema.svg)

## Resumen de la estrategia

- Las Bandas de Bollinger aportan tres líneas desde un solo bloque de indicador: banda superior, banda inferior y la media central que sirve de nivel de salida.
- Del Stochastic solo se usa la línea %K; la línea %D queda deliberadamente sin conectar, igual que en la estrategia original.
- Solo se entra desde posición plana, así que el diagrama nunca promedia una operación ya abierta.
- La estrategia original espera además un número fijo de velas entre operaciones; ese contador no tiene equivalente en bloques y se omite, por lo que este diagrama opera con más frecuencia que el código fuente.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre está en la banda inferior de Bollinger o por debajo, %K está bajo el nivel de sobreventa y la posición es plana. La orden compra un lote y abre un largo.
- **Entrada en corto**: El cierre está en la banda superior de Bollinger o por encima, %K está sobre el nivel de sobrecompra y la posición es plana. La orden vende un lote y abre un corto.
- **Salida**: El largo se cierra cuando el cierre supera la banda central y el corto cuando cae por debajo de ella. Ambas salidas usan bloques de modificación de posición en modo cierre: calculan el volumen a partir de la posición abierta y permanecen inactivos si no hay nada que cerrar. No hay stops ni objetivos, exactamente como en el código original.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Bollinger Length | 20 | Periodo de suavizado de las Bandas de Bollinger, que además fija la línea central usada para salir. |
| Bollinger Width | 2 | Multiplicador de la desviación estándar que separa las bandas de la línea central. |
| %K Oversold | 20 | Nivel por debajo del cual la línea %K confirma una compra. |
| %K Overbought | 80 | Nivel por encima del cual la línea %K confirma una venta. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- Un único bloque de velas alimenta las Bandas de Bollinger, el Stochastic y un conversor que extrae el precio de cierre.
- Los bloques conversores separan los indicadores en líneas sueltas: banda superior, inferior, central y %K.
- Cada Y lógica une una condición de banda, una condición del Stochastic y la comprobación de posición plana antes de disparar un bloque de modificación de posición en modo apertura.
- Los dos bloques de salida se disparan directamente desde las comparaciones con la banda central; el propio modo de cierre del bloque decide si hace falta una orden.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
