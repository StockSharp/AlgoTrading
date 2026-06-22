# Estrategia de Reapertura de Posiciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un puerto StockSharp del ejemplo MQL5 `Exp_ReOpenPositions`. Demuestra cómo reabrir posiciones cuando la operación actual se vuelve rentable.

## Lógica

1. La estrategia abre una posición larga inicial al inicio.
2. Cuando el precio avanza `ProfitThreshold` puntos desde el último precio de entrada, se abre otra posición larga.
3. Cada nueva entrada actualiza los niveles de stop loss y take profit relativos a su propio precio.
4. Si el precio alcanza el stop loss o el take profit, se cierran todas las posiciones y el ciclo se reinicia.

Las mismas reglas funcionan para operaciones cortas si la primera posición es corta.

## Parámetros

- `ProfitThreshold` – movimiento del precio en puntos necesario para añadir una nueva posición.
- `MaxPositions` – número máximo de posiciones abiertas.
- `StopLossPoints` – distancia desde la entrada hasta el stop de protección.
- `TakeProfitPoints` – distancia desde la entrada hasta el beneficio objetivo.
- `CandleType` – tipo de datos de velas para el procesamiento.

## Notas

El ejemplo está simplificado con fines educativos y no gestiona el volumen de operaciones ni la gestión monetaria como en el script original.
