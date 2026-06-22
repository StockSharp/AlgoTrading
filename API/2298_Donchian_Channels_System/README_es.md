# Sistema de Canales Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Sistema de Canales Donchian** opera rupturas del Canal Donchian con un desplazamiento opcional para evitar el sesgo de anticipación.

## Cómo Funciona
- **Entrada larga**: cuando el precio de cierre cruza al alza la banda superior de Donchian calculada hace `Shift` barras.
- **Entrada corta**: cuando el precio de cierre cruza a la baja la banda inferior de Donchian calculada hace `Shift` barras.
- Las posiciones se revierten en la ruptura opuesta.

## Parámetros
- `DonchianPeriod` = 20
- `Shift` = 2
- `CandleType` = 4h

## Indicadores
- Canal Donchian
