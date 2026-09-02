# Diagrama de la estrategia de reversión de tres velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dos velas empujan el mercado a la baja, la segunda marcando un mínimo más bajo que la primera, y luego una tercera gira y cierra por encima del máximo de la segunda. Esa secuencia dice que los vendedores gastaron su último empuje y recibieron una respuesta completa, y el diagrama la compra. La figura espejo se vende. Después, una media móvil simple del precio de cierre lleva la operación y decide cuándo ha terminado.

![schema](schema.svg)

## Resumen de la estrategia

- Dos bloques de patrón de velas llevan cada uno una fórmula de tres velas, así toda la figura se reconoce en un bloque en lugar de un muro de comparaciones.
- La fórmula larga pide una vela bajista, luego una vela bajista con un mínimo inferior y después una vela alcista que cierre por encima del máximo de la vela intermedia.
- La fórmula corta es el espejo exacto: alcista, alcista con un máximo superior y luego bajista cerrando por debajo del mínimo de la vela intermedia.
- La media móvil simple no interviene en la entrada: es solo la línea en la que se abandona la operación, igual que en la estrategia original.

## Reglas de entrada y salida

- **Entrada en largo**: El bloque del patrón alcista informa de la reversión de tres velas completada y la posición está plana. La orden compra un lote y abre un largo.
- **Entrada en corto**: El bloque del patrón bajista informa de la reversión espejo completada y la posición está plana. La orden vende un lote y abre un corto.
- **Salida**: El largo se cierra cuando una vela cierra por debajo de la media móvil y el corto cuando cierra por encima, ambos mediante bloques de modificación de posición en modo cierre, exactamente como en el original. El código original no tiene ni stop de pérdidas ni toma de beneficios, así que el diagrama tampoco. Lo que se ha dejado fuera es la pausa de varios cientos de velas que el original mantiene tras cada operación: un contador de barras solo se construye devolviendo una señal al propio diagrama, lo que cerraría el grafo en un bucle, así que aquí se toma cada patrón que aparece. Por eso opera bastante más a menudo que el original.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SMA Length | 20 | Periodo de suavizado de la media móvil simple que cierra las operaciones. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. La estrategia original usa velas de un minuto; aquí se emplean cinco minutos para ajustarse al histórico incluido y mantener legible la figura. |

## Detalles del diagrama

- El bloque de velas alimenta cuatro ramas: los dos bloques de patrón, la media móvil y un conversor que extrae el precio de cierre de la vela.
- Cada bloque de patrón contiene tres fórmulas, una por vela de la figura, y devuelve verdadero solo en la vela que la completa; los valores con prefijo p leen la vela anterior.
- El bloque de posición se compara con una constante cero y esa única protección cubre ambas entradas, de modo que un patrón produce una operación.
- Los dos bloques de entrada envían órdenes a mercado y toman el volumen de una constante compartida; los dos bloques de salida se disparan directamente desde las comparaciones con la media y solo actúan cuando hay algo que cerrar.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
