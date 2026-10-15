# Estrategia Mejorada de Filtro de Rango con ATR TP/SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina un filtro de rango personalizado con niveles de take-profit y stop-loss basados en ATR.
Las entradas ocurren cuando el precio rompe el filtro y se satisfacen todos los filtros adicionales:

- Volumen por encima del promedio
- RSI dentro de los límites configurados
- Confirmación de tendencia mediante cruce de EMA
- Mercado sin rango según la relación ATR

Las posiciones se cierran cuando se alcanza el nivel de stop-loss o take-profit basado en ATR.
