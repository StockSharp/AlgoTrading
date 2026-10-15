# Estratégia MACD de Amostra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o clássico expert MACD Sample do MetaTrader.
Utiliza um cruzamento de MACD combinado com um filtro de tendência EMA, níveis individuais de take-profit e stop-loss para operações compradas e vendidas, e um trailing stop opcional. O trading é permitido apenas dentro de uma janela de tempo configurável.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A linha MACD está abaixo de zero e cruza para cima a linha de sinal enquanto a EMA sobe.
  - **Vendido**: A linha MACD está acima de zero e cruza para baixo a linha de sinal enquanto a EMA cai.
- **Critérios de saída**:
  - Cruzamento MACD inverso.
  - Atingir os alvos individuais de take-profit ou stop-loss.
  - Trailing stop ativado.
- **Comprado/Vendido**: Ambos.
- **Valores padrão**:
  - `EMA Period` = 26
  - `MACD Open Level` = 3
  - `MACD Close Level` = 2
  - `Take Profit Long` = 50
  - `Take Profit Short` = 75
  - `Stop Loss Long` = 80
  - `Stop Loss Short` = 50
  - `Trailing Stop` = 30
  - Horário de trading: 4 a 19 UTC
- **Indicadores**: MACD, EMA
- **Período**: Candles de 1 hora por padrão
