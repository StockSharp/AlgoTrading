# Estrategia Quatro SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina tres medias móviles simples (SMA) rápidas con una SMA de largo plazo y un filtro de volumen. Se abre una posición larga cuando la SMA más rápida está por encima de la SMA media, la media está por encima de la SMA lenta, el precio está por encima de la SMA larga y el volumen supera su promedio en un multiplicador configurable. Las posiciones cortas requieren la alineación opuesta.

La posición se cierra en varias etapas: hasta tres niveles de take-profit y un stop-loss pueden cerrar partes de la operación. Una alineación inversa de las SMA también cierra la posición.

## Detalles

- **Indicadores**: SMA, Volumen
- **Marco temporal**: 4h
- **Tipo**: Seguimiento de tendencia con confirmación de volumen
- **Stops**: Tres niveles de take-profit y un stop-loss
