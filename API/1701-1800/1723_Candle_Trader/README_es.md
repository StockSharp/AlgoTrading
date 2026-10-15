# Estrategia Candle Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia Candle Trader** analiza la dirección (alcista o bajista) de las últimas cuatro velas completadas para identificar oportunidades de reversión a corto plazo. Opera sobre un único instrumento y envía órdenes de mercado con niveles predefinidos de take profit y stop loss.

## Lógica de la estrategia

1. **Entrada larga (directa)** – última vela alcista, las dos anteriores bajistas.
2. **Entrada larga (continuación)** – última vela alcista, la anterior bajista, las dos anteriores alcistas. Esta regla se activa solo cuando *Continuation* es `true`.
3. **Entrada corta (directa)** – última vela bajista, las dos anteriores alcistas.
4. **Entrada corta (continuación)** – última vela bajista, la anterior alcista, las dos anteriores bajistas. Se activa solo cuando *Continuation* es `true`.
5. Si *Reverse Close* está habilitado y aparece una nueva señal opuesta a la posición actual, la estrategia cierra la posición existente antes de abrir una nueva.
6. Todas las órdenes están protegidas por valores fijos de take profit y stop loss medidos en pasos de precio.

## Parámetros

| Nombre | Descripción |
|------|-------------|
| `Volume` | Volumen de la orden para cada operación. |
| `TakeProfitTicks` | Distancia del take profit en pasos de precio. |
| `StopLossTicks` | Distancia del stop loss en pasos de precio. |
| `Continuation` | Activa los patrones de continuación para entradas adicionales. |
| `ReverseClose` | Cierra una posición abierta antes de entrar en la dirección opuesta. |
| `CandleType` | Marco temporal de velas utilizado para el análisis. |

## Notas

- La estrategia evalúa únicamente velas finalizadas.
- Utiliza órdenes de mercado y cancela cualquier orden activa antes de enviar nuevas.
- Los niveles de stop loss y take profit se aplican mediante `StartProtection`.
- El tamaño de la posición puede optimizarse a través del parámetro `Volume`.
