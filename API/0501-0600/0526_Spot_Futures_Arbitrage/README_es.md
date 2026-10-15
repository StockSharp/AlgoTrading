# Arbitraje Spot-Futuros
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Arbitra la diferencia de precio entre un activo spot y su contrato de futuros.
Entra largo en spot/corto en futuros cuando el futuro cotiza por encima del spot en un umbral, y lo opuesto cuando está por debajo.
Los umbrales pueden ser dinámicos basados en la media y desviación estándar del spread, y las operaciones se cierran cuando el spread revierte o tras un tiempo máximo de mantenimiento.

## Parámetros
- **Spot** — activo spot.
- **Future** — activo de futuros.
- **CandleType** — marco temporal de la vela.
- **MinSpreadPct** — porcentaje mínimo de spread para entrar.
- **LookbackPeriod** — período para estadísticas del spread.
- **AdaptiveThreshold** — activar umbrales dinámicos.
- **MaxHoldHours** — tiempo máximo de mantenimiento de la posición en horas.
