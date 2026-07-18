# Estrategia de Reversión de Tendencia Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia construye un canal Fibonacci utilizando máximos y mínimos recientes. Se abre una posición cuando el precio cruza el nivel del 50% en la dirección de ruptura. El control de riesgo se basa en un stop loss basado en ATR y toma de ganancias con relación riesgo/beneficio, con salida parcial opcional.

## Parámetros
- **Candle Type** — serie de velas.
- **Sensitivity** — sensibilidad base para el cálculo del canal.
- **ATR Period** — longitud del ATR para el stop loss.
- **ATR Multiplier** — factor ATR para el stop loss.
- **Risk Reward** — múltiplo de ganancia sobre el riesgo.
- **Use Partial TP** — cerrar la mitad de la posición en el primer objetivo.
- **Trade Direction** — dirección de operación permitida.
