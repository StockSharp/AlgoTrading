# Estrategia de Noticias Simple
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia coloca órdenes stop pendientes alrededor de un momento de noticia especificado para capturar movimientos bruscos causados por comunicados de noticias.

## Cómo funciona

- Comenzando cinco minutos antes de `NewsTime`, la estrategia envía pares de órdenes buy stop y sell stop.
- El primer par se coloca a `Distance` pips del precio ask y bid actuales.
- Los pares adicionales se desplazan `Delta` pips respecto a los anteriores, con un total de `Deals` pares.
- Diez minutos después del comunicado de la noticia, la estrategia cancela todas las órdenes que no se hayan activado.
- Cuando se abre una posición, la estrategia monitorea los niveles de stop-loss, take-profit y trailing stop. Si se alcanza cualquier nivel, la posición se cierra.

## Parámetros

- `NewsTime` – momento del comunicado de la noticia.
- `Deals` – número de pares de órdenes buy/sell stop.
- `Delta` – espaciado entre órdenes en pips.
- `Distance` – distancia desde el precio actual para el primer par en pips.
- `StopLoss` – stop-loss inicial en pips.
- `Trail` – trailing stop en pips.
- `TakeProfit` – take-profit en pips.
- `Volume` – volumen de la orden.

## Notas

La estrategia no depende de indicadores y funciona exclusivamente con datos de nivel 1. Está destinada para fines de demostración y puede requerir ajustes para el trading real.
