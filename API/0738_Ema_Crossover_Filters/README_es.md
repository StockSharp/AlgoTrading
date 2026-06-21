# Estrategia de Cruce EMA con Filtros
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza múltiples medias móviles exponenciales (EMA) para operar cruces con filtros de tendencia adicionales.

La estrategia compra cuando la EMA de 100 cruza por encima de la EMA de 200 mientras la EMA de 9 está por encima de la EMA de 50. Vende en corto cuando la EMA de 100 cruza por debajo de la EMA de 200 y la EMA de 9 está por debajo de la EMA de 50. Las posiciones largas salen cuando la EMA de 100 cruza por debajo de la EMA de 50; las posiciones cortas salen cuando la EMA de 100 cruza por encima de la EMA de 50.

## Parámetros
- Tipo de vela
- Longitud EMA 9
- Longitud EMA 50
- Longitud EMA 100
- Longitud EMA 200
