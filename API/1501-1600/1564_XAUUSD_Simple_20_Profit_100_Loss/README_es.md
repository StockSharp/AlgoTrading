# Estrategia Simple XAUUSD con 20 de Beneficio y 100 de Pérdida
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre una posición larga cuando no hay posición abierta y ambos temporizadores de enfriamiento están inactivos.
Cierra la posición cuando el beneficio no realizado alcanza $20 o la pérdida llega a $100.
Después de una salida rentable, la estrategia espera 15 minutos antes de volver a entrar, y después de una salida con pérdida espera 12 horas.

## Parámetros

- `ProfitTarget` – objetivo de beneficio en USD.
- `LossLimit` – pérdida máxima en USD.
- `TradeCooldown` – tiempo de espera tras una pérdida.
- `EntryCooldown` – tiempo de espera tras un beneficio.
- `CandleType` – marco temporal de velas.
