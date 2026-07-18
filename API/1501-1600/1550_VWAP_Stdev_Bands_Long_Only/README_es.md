# Estrategia de Bandas VWAP Stdev (Solo Largos)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Compra cuando el precio cruza por debajo de la banda inferior de desviación estándar del VWAP y cierra al alcanzar el objetivo de beneficio.

## Parámetros

- **DevUp**: Multiplicador de desviación estándar por encima del VWAP.
- **DevDown**: Multiplicador de desviación estándar por debajo del VWAP.
- **ProfitTarget**: Objetivo de beneficio en unidades de precio.
- **GapMinutes**: Pausa antes de nueva orden en minutos.
- **CandleType**: Tipo de velas.
