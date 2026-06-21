# Estrategia GO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula un valor compuesto **GO** basado en medias móviles exponenciales (EMA) de los precios de apertura, máximo, mínimo y cierre multiplicados por el volumen. Las decisiones de trading se toman según el signo y el nivel del valor GO.

## Fórmula

`GO = ((C - O) + (H - O) + (L - O) + (C - L) + (C - H)) * V`

Donde:
- `C`, `O`, `H`, `L` – valores EMA de los precios de Cierre, Apertura, Máximo y Mínimo.
- `V` – volumen de la vela procesada.

## Reglas de trading

- **Abrir Largo**: GO > `OpenLevel`
- **Abrir Corto**: GO < `-OpenLevel`
- **Cerrar Largo**: GO < (`OpenLevel` - `CloseLevelDiff`)
- **Cerrar Corto**: GO > -(`OpenLevel` - `CloseLevelDiff`)

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `MaPeriod` | Período EMA para el suavizado de precios. |
| `OpenLevel` | Nivel GO para activar nuevas posiciones. |
| `CloseLevelDiff` | Diferencia entre los niveles de apertura y cierre. |
| `ShowGo` | Si se registran los valores GO. |
| `CandleType` | Tipo de velas utilizadas para el procesamiento. |

La estrategia opera en velas terminadas y utiliza órdenes de mercado para la gestión de posiciones.
