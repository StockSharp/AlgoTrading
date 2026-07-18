# Estrategia de Órdenes Pendientes en Noticias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia coloca un par de órdenes stop pendientes alrededor del precio actual y las gestiona a medida que el mercado evoluciona. Está destinada a operar durante publicaciones de noticias donde se esperan movimientos bruscos.

## Cómo funciona

- Cuando está sin posición, la estrategia coloca:
  - Una orden de **buy stop** en `Ask + Step`.
  - Una orden de **sell stop** en `Bid - Step`.
- Las órdenes pendientes se reprician cada `TimeModify` segundos si el mercado se ha movido al menos `StepTrail`.
- Cuando se ejecuta una orden, la orden pendiente opuesta se cancela.
- Se crean un stop loss protector y un take profit opcional basados en el precio de entrada.
- El stop loss puede moverse al punto de equilibrio tras una ganancia definida y luego seguir el precio a medida que avanza.

La estrategia opera con datos de Nivel1 y no depende de ningún indicador.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `Step` | 10 | Distancia en ticks para colocar las órdenes stop pendientes. |
| `StopLoss` | 10 | Stop loss inicial en ticks. |
| `TakeProfit` | 50 | Take profit en ticks (0 lo desactiva). |
| `TrailingStop` | 10 | Distancia del trailing stop en ticks. |
| `TrailingStart` | 0 | Ganancia en ticks antes de activar el trailing. |
| `StepTrail` | 2 | Cambio mínimo en el precio del stop (en ticks) para enviar una nueva orden stop. |
| `BreakEven` | false | Mover el stop a la entrada al alcanzar `MinProfitBreakEven`. |
| `MinProfitBreakEven` | 0 | Ganancia en ticks requerida para mover el stop al punto de equilibrio. |
| `TimeModify` | 30 | Segundos entre intentos de repricio de órdenes pendientes. |

## Notas

- Las órdenes se gestionan mediante la API de alto nivel de StockSharp.
- La estrategia cancela las órdenes protectoras cuando la posición se cierra.
- Solo se proporciona la versión en C#; no se incluye implementación en Python.
