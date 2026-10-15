# Estrategia de Momentum Color AMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el Asesor Experto de MetaTrader *Exp_ColorMomentum_AMA* a StockSharp.
Calcula el momentum del precio durante un período configurable y lo suaviza con la Media Móvil Adaptativa de Kaufman (AMA).
Las señales de trading se generan cuando el momentum suavizado muestra dos subidas o bajadas consecutivas.

## Lógica
- **Entrada larga**: El Momentum AMA sube dos barras seguidas. Cualquier posición corta existente se cierra antes de abrir una nueva posición larga.
- **Entrada corta**: El Momentum AMA cae dos barras seguidas. Cualquier posición larga existente se cierra antes de abrir una nueva posición corta.
- Las señales opuestas cierran posiciones actuales.

## Parámetros
- Tipo de vela
- Período de momentum
- Período AMA
- Período rápido
- Período lento
- Barra de señal
