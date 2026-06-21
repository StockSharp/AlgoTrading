# Estrategia Quantum Sentiment Flux (Principiante)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia entra en largo cuando la EMA rápida cruza por encima de la EMA lenta y la diferencia entre ellas supera un umbral basado en ATR. Entra en corto con la señal opuesta. Las posiciones se cierran cuando el precio se mueve un múltiplo de ATR contra la operación o alcanza un objetivo de beneficio de dos múltiplos de ATR. Un período de enfriamiento limita la frecuencia de operaciones.

## Parámetros
- Tipo de vela
- Longitud de EMA rápida
- Longitud de EMA lenta
- Período ATR
- Multiplicador ATR
- Umbral de fuerza de MA
- Barras de enfriamiento
- Cantidad
