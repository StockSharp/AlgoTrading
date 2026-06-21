# Estrategia de Cruce de MA CANX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera cruces de EMA del precio mediano (HL2). Se abre una posición larga cuando la EMA rápida cruza por encima de la EMA lenta. Si el modo solo largo está desactivado, se abre una posición corta cuando la EMA rápida cruza por debajo de la EMA lenta. Un filtro de año de inicio impide operar antes del año especificado.

## Parámetros
- Tipo de vela
- Longitud de EMA rápida
- Multiplicador (EMA lenta = longitud rápida * multiplicador)
- Solo largo
- Año de inicio
