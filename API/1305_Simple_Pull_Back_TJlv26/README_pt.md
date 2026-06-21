# Estratégia Simples de Pull Back TJlv26
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compra quando o preço está acima da SMA longa, abaixo da SMA curta e o RSI(3) está abaixo de 30 dentro de um intervalo de datas especificado. Sai com stop-loss e take-profit baseados em porcentagem ou quando o preço está acima da SMA curta mas abaixo da mínima da vela anterior.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento > SMA longa, Fechamento < SMA curta, RSI(3) < 30, tempo entre StartDate e EndDate.
- **Critérios de saída**:
  - Stop loss: preço ≤ preço de entrada × (1 − StopLossPercent/100).
  - Take profit: preço ≥ preço de entrada × (1 + TakeProfitPercent/100).
  - Fechar se preço > SMA curta e preço < mínima da vela anterior.
- **Indicadores**: SMA, RSI.
- **Stops**: Sim.
- **Direção**: Somente comprado.
