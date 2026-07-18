# Estrategia de Acción Volátil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina un rompimiento de volatilidad a corto plazo con el filtro de tendencia **Alligator** de Bill Williams calculado en el marco temporal de 4 horas.

## Reglas de trading
- **Entrada larga** cuando:
  - El ATR de período 1 es mayor que *Volatility Coef* multiplicado por el ATR con período *ATR Period*.
  - La vela es alcista y establece un nuevo máximo de 24 barras.
  - Las líneas del Alligator están alineadas hacia arriba (Lips > Teeth > Jaw) y tanto la apertura como el cierre están por encima de la línea Teeth.
- **Entrada corta** cuando las condiciones anteriores se invierten en la dirección opuesta.

Al entrar, la estrategia establece niveles de stop-loss y take-profit como múltiplos del ATR(1):
- Stop-loss = precio de entrada ± *Stop Coef* × ATR(1)
- Take-profit = precio de entrada ± *Profit Coef* × ATR(1)

## Parámetros
- **Volatility Coef** – multiplicador que compara el ATR rápido con el ATR lento.
- **ATR Period** – período del ATR lento.
- **Stop Coef** – multiplicador ATR para el stop-loss.
- **Profit Coef** – multiplicador ATR para el take-profit.
- **Candle Type** – marco temporal para el análisis principal (el Alligator usa velas de 4H).
