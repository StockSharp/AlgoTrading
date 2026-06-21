# Estrategia Ultimate Scalping v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de scalping que combina EMAs rápidas y lentas con VWAP. Filtros opcionales de vela envolvente y pico de volumen refinan las entradas. Las posiciones usan stops basados en ATR y pueden cerrarse en señales opuestas.

## Detalles

- **Largo**: EMA rápida cruza por encima de la EMA lenta y el precio está por encima del VWAP.
- **Corto**: EMA rápida cruza por debajo de la EMA lenta y el precio está por debajo del VWAP.
- **Indicadores**: EMA, VWAP, ATR, SMA.
- **Stops**: múltiplos de ATR.
