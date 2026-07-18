# Estrategia de Cruce de EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera el cruce de dos medias móviles exponenciales (EMA).
Se abre una posición larga cuando la EMA rápida cruza por encima de la EMA lenta, mientras que se abre una posición corta cuando la EMA rápida cruza por debajo de la EMA lenta.
El parámetro **Reverse** intercambia los roles de las EMA, invirtiendo efectivamente las señales de entrada.

Cada posición está protegida por niveles fijos de **Take Profit** y **Stop Loss**.
Un **Trailing Stop** opcional sigue al precio una vez que se mueve en la dirección favorable, asegurando ganancias.

La estrategia procesa únicamente velas terminadas y utiliza enlace de API de alto nivel para indicadores y suscripciones de velas.

## Parámetros
- Tipo de vela
- Longitud de EMA rápida
- Longitud de EMA lenta
- Take profit
- Stop loss
- Trailing stop
- Reverse
