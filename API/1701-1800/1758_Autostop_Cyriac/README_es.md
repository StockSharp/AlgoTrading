# Estrategia Autostop Cyriac
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utilitaria adjunta automáticamente un take profit y un stop loss a cada operación mientras está activa. No crea entradas ni salidas por sí misma y puede combinarse con trading manual u otras estrategias.

## Detalles

- **Criterios de entrada**: Ninguno. Las posiciones se abren manualmente o mediante lógica externa.
- **Largo/Corto**: Se admiten posiciones tanto largas como cortas.
- **Criterios de salida**: Las posiciones se cierran por el take profit o stop loss adjuntos.
- **Stops**: Sí. Desplazamientos de precio absolutos para take profit y stop loss mediante `StartProtection`.
- **Filtros**: Ninguno.

La estrategia expone dos parámetros:

- `TakeProfit` – distancia al take profit en unidades de precio.
- `StopLoss` – distancia al stop loss en unidades de precio.
