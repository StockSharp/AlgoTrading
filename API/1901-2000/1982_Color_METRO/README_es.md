# Estrategia ColorMETRO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el indicador ColorMETRO, que construye líneas escalonadas rápidas y lentas alrededor del RSI.
Se abre una posición larga cuando la línea rápida cruza por encima de la línea lenta. Se abre una posición corta cuando la línea rápida cruza por debajo de la línea lenta. Las posiciones opuestas se cierran con las mismas señales.

## Parámetros
- **Candle Type** – tipo de vela utilizado para los cálculos.
- **RSI Period** – período para el cálculo del RSI.
- **Fast Step** – tamaño del paso para la línea rápida.
- **Slow Step** – tamaño del paso para la línea lenta.
- **Stop Loss** – distancia en puntos para la protección de stop-loss.
- **Take Profit** – distancia en puntos para la protección de take-profit.
- **Allow Buy** – permiso para abrir posiciones largas.
- **Allow Sell** – permiso para abrir posiciones cortas.
- **Close Long** – permiso para cerrar posiciones largas.
- **Close Short** – permiso para cerrar posiciones cortas.

La estrategia usa `StartProtection` para gestionar los niveles de stop-loss y take-profit.
