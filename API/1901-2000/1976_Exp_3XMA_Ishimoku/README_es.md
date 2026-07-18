# Estrategia Exp 3XMA Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión del experto MQL `exp_3xma_ishimoku`. Utiliza el indicador Ichimoku con periodos reducidos y actúa de manera contraria a las rupturas de la nube.

La línea Kijun se compara con los límites de la nube Ichimoku. Cuando Kijun cae desde arriba de la nube hacia su interior, la estrategia cierra posiciones cortas y abre una posición larga si está permitido comprar. Cuando Kijun sube desde abajo de la nube hacia su interior, las posiciones largas se cierran y se puede abrir una posición corta.

El marco temporal predeterminado para el análisis son velas de 4 horas.

## Parámetros
- **Tenkan Period** – longitud de la línea Tenkan-sen.
- **Kijun Period** – longitud de la línea Kijun-sen.
- **Senkou Span B Period** – periodo del segundo tramo Senkou.
- **Allow Buy** – habilitar la apertura de posiciones largas.
- **Allow Sell** – habilitar la apertura de posiciones cortas.
- **Candle Type** – serie de velas utilizada para el cálculo del indicador.

## Cómo funciona
1. Se suscribe a la serie de velas seleccionada y vincula el indicador Ichimoku.
2. Procesa únicamente velas finalizadas.
3. Detecta cuándo la línea Kijun cruza los bordes de la nube.
4. Cierra posiciones opuestas y abre una nueva en la dirección de la señal si está permitido.

## Descargo de responsabilidad
Este ejemplo es con fines educativos y no constituye asesoramiento financiero. Úselo bajo su propio riesgo.
