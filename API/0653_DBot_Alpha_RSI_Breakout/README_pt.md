# Estratégia Alpha RSI Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Usa SMA e RSI para capturar cruzamentos do RSI acima de um limiar quando o preço está acima da SMA. O trailing stop é ativado após o RSI atingir o nível de take-profit. Sai por stop loss do RSI, ao atingir o take-profit ou pelo trailing stop.

## Detalhes

- **Dados**: velas de preço.
- **Entrada**: comprar quando o RSI cruza acima do nível de entrada e o preço está acima da SMA.
- **Saída**: RSI abaixo do nível de stop, RSI atinge o take-profit ou o preço cai abaixo do trailing stop após sua ativação.
- **Instrumentos**: qualquer.
- **Risco**: stop loss baseado em RSI e trailing stop após lucro.
