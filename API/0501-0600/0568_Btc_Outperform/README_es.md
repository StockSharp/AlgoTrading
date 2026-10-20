# BTC Outperform

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Reglas de negociación

Agrupa velas diarias UTC terminadas en periodos fijos. Los semanales parten del lunes 1970-01-05 y los de 90 días de 1970-01-01. Solo publica un cierre cuando dispone de todas las velas diarias del periodo. Los periodos iniciales incompletos o con huecos no publican cierre.

Compara los últimos cierres completos observados: semanal superior compra, inferior vende e igualdad mantiene la posición. Desde cero ordena `Volume`; para revertir usa `Volume+abs(Position)` y deja una posición opuesta completa. Los periodos en formación no alteran la comparación. Las órdenes pendientes impiden duplicados. No hay stop de precio ni objetivo automático.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `CandleType` | `1d` |
| `WeeklyDays` | `7` |
| `QuarterlyDays` | `90` |
