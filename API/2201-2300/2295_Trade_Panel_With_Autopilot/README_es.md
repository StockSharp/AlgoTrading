# Estrategia Panel de Trading Con Autopiloto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el ejemplo de MQL5 **Trade panel with autopilot** al framework StockSharp.
Calcula la presión alcista y bajista en múltiples marcos temporales. Se abre una posición cuando el porcentaje correspondiente supera el umbral *Open %* y se cierra cuando cae por debajo del nivel *Close %*. Opcionalmente, se puede aplicar un stop loss basado en fractales usando velas de 10 minutos.

## Parámetros

- **Autopilot** – habilitar o deshabilitar el trading automatizado.
- **Open %** – umbral de votos requerido para abrir una posición.
- **Close %** – umbral para cerrar una posición existente.
- **Use Fixed Volume** – si es verdadero, usar el valor de *Fixed Volume*.
- **Fixed Volume** – volumen de orden absoluto.
- **Volume %** – porcentaje de cartera utilizado cuando el volumen es dinámico.
- **Use Stop Loss** – habilitar stop loss basado en fractales recientes.

## Lógica

Para cada marco temporal desde 1 minuto hasta 1 mes, la estrategia compara la última vela con la anterior. Cada comparación de apertura, máximo, mínimo y promedios derivados agrega un voto de compra o venta. Los porcentajes de votos de compra y venta determinan la colocación de órdenes. Cuando está habilitado, el último fractal de las velas de 10 minutos actúa como trailing stop.

Este ejemplo tiene fines educativos y no representa asesoramiento de inversión.
