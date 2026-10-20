# Bitcoin 1H/15M Breakout

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Reglas de negociación

Opera rupturas en velas terminadas de 15 minutos del rango de la hora anterior completa. Las velas menores se agregan en horas UTC. Una hora solo es válida si contiene los cuatro cuartos consecutivos desde su inicio exacto; una hora inicial parcial o un hueco no crea rango.

Para una vela menor abierta en la hora H, usar exclusivamente el máximo/mínimo de H−1 ya completa, nunca la hora actual parcial. Sin posición, comprar `Volume` con cierre estrictamente por encima del máximo o vender `Volume` por debajo del mínimo. Se permiten ambas direcciones; una posición abierta no aumenta ni revierte por nuevas rupturas. Una orden activa bloquea otra.

La protección local nativa fija `StopLossBuffer` como distancia absoluta desde ejecuciones de entrada, predeterminado 50. El take-profit usa `StopLossBuffer * RiskRewardRatio`, predeterminado 100 con razón 2. Ambas salidas son a mercado por toda la posición y no cambian con nuevos rangos horarios. `HigherTimeFrame` debe ser múltiplo entero del intervalo menor.

La protección local nativa usa velas y actualizaciones de precio suscritas del instrumento. Las distancias parten de los precios ejecutados. Las salidas son a mercado y dependen del mercado disponible y del paso de precio.

## Parámetros

| Parámetro | Predeterminado |
|---|---|
| `CandleType` | `15m` |
| `HigherTimeFrame` | `1h` |
| `StopLossBuffer` | `50` |
| `RiskRewardRatio` | `2` |
