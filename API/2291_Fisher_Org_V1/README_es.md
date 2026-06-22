# Estrategia Fisher Org v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza el indicador Fisher Transform para capturar reversiones de tendencia. Se abre una posición larga cuando el indicador forma un mínimo local, mientras que se abre una posición corta cuando aparece un máximo local. Las señales opuestas cierran cualquier posición existente.

## Reglas
- **Largo**: `Fisher[t-2] > Fisher[t-1]` y `Fisher[t-1] <= Fisher[t]`
- **Corto**: `Fisher[t-2] < Fisher[t-1]` y `Fisher[t-1] >= Fisher[t]`

## Parámetros
- `Fisher Length` – período del Fisher Transform (por defecto 7)
- `Candle Type` – marco temporal de las velas utilizadas para los cálculos

## Indicadores
- Fisher Transform
