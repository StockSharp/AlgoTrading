# Estrategia de Fuerza Compradora/Vendedora RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia calcula el RSI sobre las velas entrantes y lo suaviza con una EMA.
Deriva dos líneas, `cc` y `bb`, que representan la presión compradora y vendedora.
Se abre una posición larga cuando `cc` cruza por encima de `bb`, mientras que se abre una posición corta cuando `cc` cruza por debajo de `bb`.
