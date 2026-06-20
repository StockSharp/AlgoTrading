# Estrategia Williams Alligator ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el indicador Williams Alligator combinado con un stop-loss basado en ATR. Se abre una posición larga cuando la línea Lips cruza por encima de la línea Jaw. La posición se cierra cuando Lips cruza por debajo de Jaw o cuando el precio cae hasta un nivel de stop basado en ATR.

## Detalles
- **Criterios de entrada**: Lips cruza por encima de Jaw.
- **Criterios de salida**: Lips cruza por debajo de Jaw o stop-loss por ATR.
- **Indicadores**: Smoothed Moving Averages, Average True Range.
- **Tipo**: Solo largos.
