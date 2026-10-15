# Estrategia ColorMETRO XRSX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una implementación en StockSharp inspirada en el Expert Advisor original de MQL5 "Exp_ColorMETRO_XRSX". Utiliza dos medias móviles suavizadas para detectar cambios de tendencia. Se abre una posición larga cuando la media rápida cruza por encima de la media lenta, mientras que se abre una posición corta cuando la media rápida cruza por debajo de la media lenta.

## Parámetros

- **Fast Period** – período de la media móvil rápida.
- **Slow Period** – período de la media móvil lenta.
- **Candle Type** – marco temporal de las velas utilizadas para los cálculos.

## Cómo Funciona

1. La estrategia se suscribe a la serie de velas seleccionada.
2. Se calculan dos indicadores `Sma` con diferentes períodos sobre el precio de cierre.
3. Cuando la SMA rápida cruza por encima de la SMA lenta, se cierra cualquier posición corta y se abre una posición larga.
4. Cuando la SMA rápida cruza por debajo de la SMA lenta, se cierra cualquier posición larga y se abre una posición corta.
5. Los valores previos de las medias se almacenan para detectar cruces solo una vez.

## Notas

- La estrategia utiliza la API de alto nivel con `Bind` para el procesamiento de indicadores.
- `StartProtection` está habilitado para gestionar los mecanismos de protección.
- Esta implementación es una traducción simplificada y no utiliza el indicador personalizado original.
