# Estrategia Expert MACD EURUSD 1 Hora
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una traducción en C# del asesor experto de MetaTrader 5 **Expert MACD EURUSD 1 Hour**. Opera en velas de una hora usando el indicador MACD con períodos corto, largo y de señal de **5 / 15 / 3**. La estrategia busca un fuerte cambio de momentum donde la línea principal del MACD cruza por encima o por debajo del nivel cero mientras la línea de señal confirma el movimiento. Se usa un trailing stop para proteger las posiciones abiertas, y las operaciones se cierran cuando la pendiente del MACD se vuelve contra la posición actual.

## Parámetros

- `FastLength` – período de la EMA rápida para MACD (predeterminado: 5).
- `SlowLength` – período de la EMA lenta para MACD (predeterminado: 15).
- `SignalLength` – período de la línea de señal para MACD (predeterminado: 3).
- `TrailingPoints` – distancia del trailing stop en puntos de precio (predeterminado: 25).
- `CandleType` – marco temporal de las velas (predeterminado: 1 hora).
- La propiedad `Volume` de la estrategia controla el tamaño de la orden.

## Lógica de negociación

### Entrada larga
1. Valores de la línea de señal: `mac8 > mac7 > mac6` y `mac6 < mac5` (línea de señal subiendo).
2. Valores de la línea principal: `mac4 > mac3 < mac2 < mac1` (línea principal subiendo tras una caída).
3. `mac2 < -0.00020`, `mac4 < 0` y `mac1 > 0.00020` – línea principal cruza por encima de cero.
4. Si todas las condiciones se cumplen y no hay posición larga abierta, comprar a mercado.

### Entrada corta
1. Valores de la línea de señal: `mac8 < mac7 < mac6` y `mac6 > mac5` (línea de señal bajando).
2. Valores de la línea principal: `mac4 < mac3 > mac2 > mac1` (línea principal bajando tras un pico).
3. `mac2 > 0.00020`, `mac4 > 0` y `mac1 < -0.00035` – línea principal cruza por debajo de cero.
4. Si todas las condiciones se cumplen y no hay posición corta abierta, vender a mercado.

### Reglas de salida
- Cerrar un largo cuando el valor principal actual está por debajo del anterior.
- Cerrar un corto cuando el valor principal actual está por encima del anterior.
- El trailing stop se actualiza en cada vela y sale si el precio cruza el nivel del stop.

## Notas

Este ejemplo demuestra el uso de la API de alto nivel de StockSharp con vinculación de indicadores y gestión manual del trailing stop. Está destinado a fines educativos y no incluye gestión de dinero más allá del parámetro fijo `Volume`.
