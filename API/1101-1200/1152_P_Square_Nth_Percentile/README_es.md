# Estrategia P-Square del N-ésimo Percentil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estima el percentil seleccionado de la serie fuente utilizando el algoritmo P-Square. Abre una posición larga cuando el valor supera el percentil superior y una posición corta cuando el valor cae por debajo del percentil inferior.

## Parámetros
- `Percentile` – percentil a estimar.
- `UseReturns` – procesar rendimientos en lugar de precios.
- `CandleType` – tipo de datos de vela.
