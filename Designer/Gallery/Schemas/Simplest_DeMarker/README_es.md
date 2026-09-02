# Diagrama de la estrategia DeMarker más simple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

DeMarker mide cuánto se extiende cada vela más allá de la anterior, hacia arriba frente a hacia abajo, y devuelve un valor entre 0 y 1. Este diagrama no compra el extremo, sino el regreso desde él: una lectura que sube desde debajo del nivel de sobreventa hasta ese nivel compra, y una que baja desde encima del nivel de sobrecompra hasta él vende. La estrategia original usa velas horarias y espera cuatro velas entre operaciones; el diagrama trabaja en cinco minutos y omite la pausa, ya que el control de la posición impide una segunda entrada en el mismo sentido.

![schema](schema.svg)

## Resumen de la estrategia

- DeMarker se calcula sobre velas cerradas de un solo instrumento y siempre queda entre 0 y 1, con 0.5 como centro neutro.
- Un bloque de valor anterior guarda la lectura de la vela previa, de modo que el diagrama reacciona al regreso a la zona neutra y no a permanecer en ella.
- La posición actual entra en ambas decisiones: solo se compra mientras no sea larga y solo se vende mientras no sea corta.
- La pausa de cuatro velas del original no se reproduce; puede añadirse después sin tocar la parte de señales.

## Reglas de entrada y salida

- **Entrada en largo**: La lectura anterior del DeMarker estaba por debajo del nivel de sobreventa, la actual está en ese nivel o por encima y la posición no es larga. La orden compra un lote: abre un largo desde plano o cierra un corto existente.
- **Entrada en corto**: La lectura anterior del DeMarker estaba por encima del nivel de sobrecompra, la actual está en ese nivel o por debajo y la posición no es corta. La orden vende un lote: abre un corto desde plano o cierra un largo existente.
- **Salida**: No hay bloque de salida ni stop de protección, igual que en la estrategia original: la señal contraria deja la posición en cero, porque todas las órdenes usan el mismo volumen.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| DeMarker Length | 14 | Periodo de suavizado del oscilador DeMarker. |
| Oversold | 0.2 | Nivel de sobreventa; regresar hasta él desde abajo es la señal de compra. |
| Overbought | 0.8 | Nivel de sobrecompra; regresar hasta él desde arriba es la señal de venta. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas de todo el diagrama; el original usaba una hora. |

## Detalles del diagrama

- El bloque de velas alimenta el bloque de indicador con DeMarker, y el bloque de valor anterior toma esa misma salida una vela atrás.
- Cuatro bloques de comparación construyen los dos regresos: la lectura anterior más allá de un nivel y la actual de vuelta en él.
- Otros dos bloques comparan la posición con una constante cero, y cada Y lógica reúne tres condiciones en una señal.
- Ambos bloques de modificación de posición envían órdenes a mercado y toman el volumen de una única constante compartida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
