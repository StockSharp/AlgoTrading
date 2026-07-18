# Estratégia de Canais com NVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina Bandas de Bollinger ou Canais de Keltner com o Índice de Volume Negativo (NVI). Uma posição comprada é aberta quando o preço fecha abaixo da banda inferior enquanto o NVI está acima de sua EMA. A posição é fechada quando o NVI cai abaixo de sua EMA. Percentuais opcionais de stop-loss e take-profit estão disponíveis.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento < banda inferior e NVI > EMA do NVI.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - **Comprado**: NVI < EMA do NVI.
- **Stops**: Opcional, percentual do preço de entrada.
- **Valores padrão**:
  - `ChannelType` = "BB"
  - `ChannelLength` = 20
  - `ChannelMultiplier` = 2
  - `NviEmaLength` = 200
  - `EnableStopLoss` = false
  - `StopLossPercent` = 0
  - `EnableTakeProfit` = false
  - `TakeProfitPercent` = 0
- **Filtros**:
  - Categoria: Canal
  - Direção: Somente comprado
  - Indicadores: Bollinger Bands ou Keltner Channels, EMA, NVI
  - Stops: Opcional
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
