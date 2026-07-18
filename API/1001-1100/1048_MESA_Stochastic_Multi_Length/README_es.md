# Estrategia MESA Stochastic Multi Length
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza cuatro osciladores MESA Stochastic con diferentes longitudes de retrospección. Se abre una posición larga cuando los cuatro osciladores están por encima de su disparador de media móvil. Se abre una posición corta cuando los cuatro osciladores caen por debajo de sus disparadores.

## Parámetros
- `Length1` – retrospección del primer oscilador.
- `Length2` – retrospección del segundo oscilador.
- `Length3` – retrospección del tercer oscilador.
- `Length4` – retrospección del cuarto oscilador.
- `TriggerLength` – período de suavizado para las medias móviles disparadoras.
- `CandleType` – marco temporal de las velas.
