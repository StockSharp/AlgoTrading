# Estrategia Millenium Code
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Millenium Code** es un sistema posicional que abre como máximo una operación por día. La dirección se determina mediante un cruce de medias móviles filtrado por máximos y mínimos recientes. Las operaciones se colocan a una hora definida por el usuario y se cierran por tiempo, stop loss, take profit o duración máxima.

## Lógica de Operación

1. En el tiempo de apertura especificado, la estrategia verifica si el trading está permitido para el día de la semana actual.
2. Se comparan las medias móviles simples rápida y lenta. Si la MA rápida cruza por encima de la MA lenta y el precio confirma el rompimiento, se abre una posición larga. Las condiciones opuestas abren una posición corta.
3. Solo se permite una operación por día. Las señales posteriores se ignoran hasta el siguiente día de trading.
4. Las posiciones se cierran cuando:
   - Se alcanza el nivel de stop loss o take profit.
   - Ocurre el tiempo de cierre configurado.
   - Se supera la duración máxima de la operación.

## Parámetros

- **Candle Type** – marco temporal de las velas de entrada.
- **Fast MA** – período de la media móvil rápida.
- **Slow MA** – período de la media móvil lenta.
- **HighLow Bars** – número de velas utilizadas para buscar máximos y mínimos recientes.
- **Reverse** – invertir las señales de compra/venta.
- **Stop Loss** – distancia al stop loss en pasos de precio.
- **Take Profit** – distancia al take profit en pasos de precio.
- **Open Hour/Minute** – hora para comenzar a buscar entradas (-1 deshabilita).
- **Close Hour/Minute** – hora para cerrar posiciones (-1 deshabilita).
- **Duration** – vida máxima de la operación en horas (0 deshabilita).
- **Sunday ... Friday** – habilitar el trading para cada día de la semana.

## Notas

Esta estrategia utiliza únicamente características de API de alto nivel y evita acceder directamente al historial del indicador. Está destinada como ejemplo educativo y no como asesoramiento de inversión.
