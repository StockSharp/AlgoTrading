# Estrategia de Ajuste Fino de Entradas Gann + Oscilador de Zona de Volumen Suavizado por Laplace
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza un oscilador de volumen suavizado por medias móviles exponenciales.
Se abre una posición larga cuando el oscilador suavizado sube por encima del umbral.
Se abre una posición corta cuando cae por debajo del umbral negativo.
Si las señales desaparecen y **Close All** está habilitado, cualquier posición abierta se cierra.

## Parámetros
- **Fast Volume EMA** – período para la media rápida de volumen.
- **Slow Volume EMA** – período para la media lenta de volumen.
- **Smooth Length** – período de suavizado del oscilador.
- **Threshold** – nivel de señal para entradas.
- **Close All** – cerrar posición cuando no hay señal.
- **Candle Type** – tipo de vela utilizado para los cálculos.
