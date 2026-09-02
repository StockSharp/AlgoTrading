# Diagrama de la estrategia de cruce del precio con la VWMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La media móvil ponderada por volumen pondera cada precio por el volumen negociado en él, así que se inclina hacia los niveles donde el dinero cambió de manos de verdad. El diagrama sigue el paso del precio de cierre por esa media: si el cierre pasa de estar por debajo a estar por encima, compra; si ocurre lo contrario, vende. La estrategia original usa velas de un minuto y descansa varias barras tras cada operación; el diagrama trabaja en cinco minutos y omite esa pausa, porque el control de la posición ya impide una segunda entrada en el mismo sentido.

![schema](schema.svg)

## Resumen de la estrategia

- VolumeWeightedMovingAverage recibe la vela completa y no solo un precio, porque también necesita el volumen negociado.
- Tanto el cierre como la media se guardan además una vela atrás, de modo que el cruce se lee igual que en el código original.
- Cada entrada está protegida por la posición: solo se compra mientras la posición no sea larga y solo se vende mientras no sea corta.
- La pausa de la estrategia original no se reproduce, así que el diagrama responde a todos los cruces que ve.

## Reglas de entrada y salida

- **Entrada en largo**: El cierre anterior estaba en la VWMA anterior o por debajo y el cierre actual está por encima de la VWMA actual, mientras la posición no es larga. La orden compra un lote: abre un largo desde plano o cierra un corto existente.
- **Entrada en corto**: El cierre anterior estaba en la VWMA anterior o por encima y el cierre actual está por debajo de la VWMA actual, mientras la posición no es corta. La orden vende un lote: abre un corto desde plano o cierra un largo existente.
- **Salida**: No hay bloque de salida propio ni stop de protección: el cruce contrario deja la posición en cero, porque todas las órdenes usan el mismo volumen.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| VWMA Length | 14 | Periodo de suavizado de la media móvil ponderada por volumen. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas de todo el diagrama; el original usaba un minuto. |

## Detalles del diagrama

- El bloque de velas alimenta dos ramas a la vez: el bloque de indicador con VolumeWeightedMovingAverage y un conversor que extrae el precio de cierre.
- Dos bloques de valor anterior conservan el cierre y la media de la vela precedente.
- Cuatro bloques de comparación forman los dos cruces, otros dos comparan la posición con una constante cero y cada Y lógica reúne tres de esas señales.
- Ambos bloques de modificación de posición envían órdenes a mercado con el volumen de una única constante compartida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
