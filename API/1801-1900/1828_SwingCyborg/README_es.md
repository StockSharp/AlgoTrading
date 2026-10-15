# Estrategia Swing Cyborg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Swing Cyborg es un asistente discrecional que automatiza la ejecución basándose en el pronóstico de tendencia del propio operador. El usuario define la dirección esperada de la tendencia y la ventana de tiempo durante la cual debe ser válida. La estrategia confirma las entradas con el indicador RSI y gestiona las salidas con objetivos fijos.

## Parámetros
- `Volume` – volumen de orden en lotes.
- `TrendPrediction` – dirección esperada de la tendencia (Uptrend o Downtrend).
- `TrendTimeframe` – marco temporal usado para RSI y trading (M30, H1 o H4).
- `TrendStart` – inicio del período de tendencia definido por el usuario.
- `TrendEnd` – fin del período de tendencia definido por el usuario.
- `Aggressiveness` – preset de gestión de dinero:
  - Bajo: take profit 300 pips, stop loss 200 pips.
  - Medio: take profit 500 pips, stop loss 250 pips.
  - Alto: take profit 600 pips, stop loss 300 pips.

## Lógica de trading
1. Esperar una nueva vela en el marco temporal seleccionado.
2. Operar solo si el tiempo actual está entre `TrendStart` y `TrendEnd`.
3. Calcular RSI(14).
4. Si no hay posición abierta:
   - Si `TrendPrediction` es Uptrend y RSI ≤ 65 → comprar.
   - Si `TrendPrediction` es Downtrend y RSI ≥ 35 → vender.
5. `StartProtection` cierra automáticamente la posición cuando la ganancia o pérdida alcanza el nivel predefinido.

La estrategia opera sobre velas terminadas y no abre una nueva posición mientras haya una activa.
