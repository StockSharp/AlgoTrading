# Estrategia de Media Móvil Simple en Múltiples Marcos Temporales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica la lógica de `simple_multiple_time_frame_moving_average.mq4`. Alinea las tendencias en dos marcos temporales utilizando medias móviles simples.

## Lógica de la Estrategia
- Calcula SMA con período `Length` en velas de 1 hora y 4 horas.
- Entra largo cuando ambas SMA están subiendo.
- Entra corto cuando ambas SMA están bajando.
- Cierra una posición larga cuando cualquiera de las SMA gira a la baja.
- Cierra una posición corta cuando cualquiera de las SMA gira al alza.
- Solo puede haber una posición activa a la vez.

## Parámetros
- **MA Length** (`Length`): período utilizado para ambas medias móviles.
- **Short Time Frame** (`ShortCandleType`): marco temporal para la primera SMA (por defecto 1 hora).
- **Long Time Frame** (`LongCandleType`): marco temporal para la segunda SMA (por defecto 4 horas).

El volumen de operaciones se toma de la propiedad `Volume` de la estrategia.

## Notas
Esta implementación se centra en las medias de una hora y cuatro horas utilizadas en la versión MQL original y omite los cálculos de marcos temporales superiores no utilizados.
