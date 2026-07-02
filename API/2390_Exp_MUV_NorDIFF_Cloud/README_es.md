# Estrategia Exp MUV NorDIFF Cloud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el momentum normalizado de SMA y EMA.
Entra en largo cuando el momentum de SMA o EMA alcanza +100 y en corto cuando llega a -100.

## Parámetros
- `MaPeriod` – período de la media móvil.
- `MomentumPeriod` – número de barras utilizadas para el cálculo del momentum.
- `KPeriod` – ventana para la normalización de los extremos del momentum.
- `CandleType` – marco temporal de las velas.

## Notas
La estrategia calcula los valores de SMA y EMA, mide su momentum y lo normaliza dentro del rango reciente para generar señales de operación.
