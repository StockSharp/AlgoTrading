# Estrategia de Panel de Trading Multicurrency
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia emula el comportamiento del asesor experto MQL original "Multicurrency trading panel". Monitorea tres pares de divisas (EURUSD, USDJPY, GBPUSD) y compara la última vela con la anterior usando siete métricas simples (apertura, máximo, mínimo, (máximo+mínimo)/2, cierre, (máximo+mínimo+cierre)/3, (máximo+mínimo+cierre+cierre)/4).
Por cada comparación, se incrementa una puntuación de COMPRA o VENTA. Cuando el trading automático está habilitado, la estrategia abre o invierte posiciones en un par si la puntuación de COMPRA supera a la de VENTA o viceversa.

## Parámetros
- **EURUSD** – primer instrumento.
- **USDJPY** – segundo instrumento.
- **GBPUSD** – tercer instrumento.
- **Candle Type** – marco temporal de velas.
- **Auto Trade** – activar/desactivar el envío automático de órdenes.

La estrategia es una demostración simplificada y no está destinada al trading real.
