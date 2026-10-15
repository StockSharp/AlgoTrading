# Estrategia de Rompimiento para Principiantes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza los precios más altos y más bajos de las últimas `Period` velas para formar un canal. Cuando el cierre se acerca al límite superior, la estrategia va largo. Cuando el cierre se acerca al límite inferior, va corto.

## Reglas de Entrada
- **Largo**: Close >= highest - (highest - lowest) * `ShiftPercent` / 100 y la tendencia aún no es alcista.
- **Corto**: Close <= lowest + (highest - lowest) * `ShiftPercent` / 100 y la tendencia aún no es bajista.

## Reglas de Salida
- La señal opuesta cierra la posición actual y abre una nueva en la otra dirección.

## Parámetros
- `Period` – barras hacia atrás para el cálculo del canal.
- `ShiftPercent` – desplazamiento porcentual desde los bordes del canal.
- `CandleType` – marco temporal de las velas de trabajo.
- `Volume` – volumen de la operación.
- `StopLoss` – stop loss en unidades de precio.
- `TakeProfit` – take profit en unidades de precio.

## Indicadores
- Highest
- Lowest
