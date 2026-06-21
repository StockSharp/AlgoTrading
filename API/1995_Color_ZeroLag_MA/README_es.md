# Estrategia Color Zero Lag MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza una media móvil de retardo cero (ZLMA) para detectar reversiones de tendencia. Abre posiciones largas cuando la ZLMA gira hacia arriba y abre posiciones cortas cuando la ZLMA gira hacia abajo. Las posiciones existentes se cierran cuando la pendiente del indicador se revierte.

## Parámetros

- **Length**: Período de la media móvil de retardo cero.
- **Candle Type**: Marco temporal para las velas utilizadas por la estrategia.
- **Open Buy**: Activar la apertura de posiciones largas.
- **Open Sell**: Activar la apertura de posiciones cortas.
- **Close Buy**: Cerrar posiciones largas cuando la ZLMA gira hacia abajo.
- **Close Sell**: Cerrar posiciones cortas cuando la ZLMA gira hacia arriba.

## Lógica

1. Suscribirse a las velas del marco temporal seleccionado.
2. Calcular la media móvil de retardo cero.
3. Rastrear los dos últimos valores de la ZLMA para determinar la dirección de la pendiente.
4. Si la pendiente cambia de bajista a alcista, cerrar posiciones cortas y abrir una posición larga.
5. Si la pendiente cambia de alcista a bajista, cerrar posiciones largas y abrir una posición corta.

Este sencillo enfoque sigue el cambio de color de la media móvil de retardo cero para capturar posibles reversiones de tendencia.
