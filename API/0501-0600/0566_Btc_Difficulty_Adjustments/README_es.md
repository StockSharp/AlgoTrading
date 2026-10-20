# BTC Difficulty Adjustments

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Reglas de negociación

Requiere `DifficultyHistoryFile` con cabecera `Time,Difficulty`. Las publicaciones deben ser tiempos ISO UTC estrictamente crecientes terminados en `Z`, con dificultad positiva. Se lee al arrancar y los datos ausentes o incorrectos producen un error explícito. La dificultad no se deduce de precios.

Cada vela terminada consume solo observaciones publicadas hasta su `OpenTime`. La primera establece la base; un valor sin cambios no genera señal. Cambio: `100*(difficulty/previous-1)`. Sin umbral, un aumento compra y una disminución vende. Con umbral solo actúa `abs(change)>=Threshold`: una caída compra y un aumento vende. La última variación observada admisible decide el lado. Abre `Volume` desde cero y revierte con `Volume+abs(Position)`. Las órdenes pendientes bloquean decisiones superpuestas. No hay stop automático.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `CandleType` | `1d` |
| `ThresholdMode` | `false` |
| `Threshold` | `10` |
| `DifficultyHistoryFile` | `obligatorio` |
