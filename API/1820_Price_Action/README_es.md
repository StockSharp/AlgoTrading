# Estrategia de Price Action
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Price Action** alterna entre órdenes largas y cortas a mercado cada vez que la posición anterior se cierra.
Aplica una distancia de stop-loss fija, un objetivo de take-profit basado en apalancamiento y un stop de seguimiento opcional que sigue el mercado con un paso configurable.

## Detalles
- **Criterios de entrada:** Sin posición abierta. La dirección alterna entre compra y venta después de cada operación.
- **Largo/Corto:** Ambos.
- **Criterios de salida:** El precio alcanza el stop de seguimiento, el stop inicial o el nivel de take-profit.
- **Stops:** Distancia de stop fija con seguimiento opcional (el paso define el movimiento mínimo de precio para la actualización).
- **Valores predeterminados:** `Volume = 1`, `TP = 100`, `Leverage = 5`, `TrailingStop = 0`, `TrailingStep = 0`, `InitialDirection = Buy`, `CandleType = TimeSpan.FromMinutes(1).TimeFrame()`.
