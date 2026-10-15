# Estrategia TPSL Insert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una traducción de StockSharp del script MetaTrader 4 **TPSL-Insert.mq4**. No genera señales de entrada ni de salida. Su único propósito es adjuntar órdenes de toma de ganancias y stop-loss a posiciones existentes.

## Cómo funciona

1. Al inicio, la estrategia lee los parámetros `TakeProfitPips` y `StopLossPips`.
2. Los valores se convierten de pips a precio usando el `PriceStep` del instrumento.
3. Se llama a `StartProtection` para colocar órdenes protectoras.
   - Si ya existe una posición, las órdenes protectoras se envían inmediatamente.
   - Las posiciones futuras abiertas por la estrategia serán protegidas automáticamente.

Este comportamiento es útil cuando las posiciones se abren manualmente o por sistemas externos y necesitas insertar límites de riesgo rápidamente.

## Parámetros

| Nombre | Descripción | Predeterminado |
|--------|-------------|----------------|
| `TakeProfitPips` | Distancia de toma de ganancias en pips. | `35` |
| `StopLossPips` | Distancia de stop-loss en pips. | `100` |

## Notas

- La estrategia no se suscribe a datos de mercado y no contiene lógica de trading.
- `StartProtection` maneja la creación y cancelación de órdenes protectoras.
