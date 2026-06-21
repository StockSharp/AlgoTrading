# Estrategia Adaptativa XRP AI de 15 m v3.1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera XRP en velas de 15 minutos usando un filtro de tendencia de marco temporal superior. Selecciona entre pequeños retrocesos, vaciados de volumen medios o grandes explosiones de impulso, y aplica stops, objetivos basados en ATR, stop trailing y una salida basada en tiempo.

## Parámetros
- **Risk Mult** – multiplicador ATR para el stop inicial.
- **Small TP** – multiplicador ATR para take profit en un pequeño retroceso.
- **Med TP** – multiplicador ATR para take profit en un vaciado de volumen medio.
- **Large TP** – multiplicador ATR para take profit en una gran explosión de impulso.
- **Volume Mult** – multiplicador de volumen SMA-20 para detectar picos.
- **Trail Percent** – porcentaje ATR del stop trailing desde el precio más alto.
- **Trail Arm** – ganancia abierta en múltiplos ATR antes de activar el trailing.
- **Max Bars** – número máximo de velas de 15 minutos para mantener una posición.
- **Candle Type** – tipo de vela usado para los cálculos principales.
- **Trend Candle Type** – tipo de vela usado para el filtro de tendencia.
