# MACD Duplo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O MACD Duplo utiliza dois indicadores MACD com velocidades diferentes. Uma posição é aberta somente quando ambos os MACDs concordam na direção.

O primeiro MACD é rápido e reage rapidamente. O segundo é mais lento e confirma a tendência antes das operações.

## Detalhes
- **Dados**: Candles de preço.
- **Critérios de entrada**:
  - **Comprado**: Ambas as linhas MACD acima de suas linhas de sinal.
  - **Vendido**: Ambas as linhas MACD abaixo de suas linhas de sinal.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Stop loss opcional.
- **Valores padrão**:
  - `FastLength1` = 12
  - `SlowLength1` = 26
  - `SignalLength1` = 9
  - `MaType1` = Ema
  - `FastLength2` = 24
  - `SlowLength2` = 52
  - `SignalLength2` = 9
  - `MaType2` = Ema
  - `StopLossPercent` = 2
  - `CandleType` = tf(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado e Vendido
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
