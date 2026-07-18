# Estrategia de Cruce del Indicador Vortex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera los cruces de las líneas positiva (VI+) y negativa (VI-) del indicador Vortex.
Cuando VI+ cruza por encima de VI-, la estrategia va largo; cuando VI- cruza por encima de VI+, va corto.
Un stop-loss y un take-profit en pasos de precio se gestionan automáticamente.

## Parámetros

- **Vortex Length** – período del indicador Vortex.
- **Candle Type** – marco temporal utilizado para el cálculo del indicador.
- **Stop Loss** – stop de protección en pasos de precio.
- **Take Profit** – beneficio objetivo en pasos de precio.

## Detalles

- **Indicadores**: Vortex
- **Dirección**: Largo y corto
- **Marco temporal**: Configurable
- **Gestión de riesgo**: Stop-loss y take-profit mediante `StartProtection`.
