# Estrategia de Cuadrícula RSI Multitemporal con Flechas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera cuando el RSI del marco temporal actual y de dos marcos temporales superiores alcanzan niveles de sobrecompra o sobreventa. La primera posición se abre cuando todos los RSI se alinean, luego se añaden posiciones adicionales usando una cuadrícula basada en ATR con un multiplicador de lote creciente. La estrategia apunta a un porcentaje de beneficio diario, se reinicia cada día y cierra en señales inversas o por drawdown.

## Parámetros
- Tipo de vela
- Longitud RSI
- Nivel de sobreventa
- Nivel de sobrecompra
- Marco temporal superior 1
- Marco temporal superior 2
- Factor de multiplicación de cuadrícula
- Factor de multiplicación de lote
- Niveles máximos de cuadrícula
- Objetivo de beneficio diario %
- Longitud ATR
