# Estrategia de Niveles con Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertida del script MQL `levels_with_trail.mq4`. La estrategia abre operaciones cuando el precio cruza un nivel especificado y puede aplicar trailing al stop-loss.

## Cómo funciona
- Se suscribe a velas del marco temporal elegido.
- Cuando no hay posición abierta y el precio de cierre está por encima de `Level Price`, compra; si el precio está por debajo, vende.
- Si `Trail Stop` está activado, el stop-loss sigue al precio cuando la posición es rentable.
- Las posiciones se cierran cuando se activan el stop-loss, el take-profit o una señal de ruptura opuesta.

## Parámetros
- `Stop Loss` – tamaño del stop-loss en unidades de precio.
- `Take Profit` – tamaño del take-profit en unidades de precio.
- `Level Price` – nivel de ruptura a vigilar.
- `Trail Stop` – activar o desactivar el trailing stop-loss.
- `Candle Type` – marco temporal de velas utilizado para el análisis.
