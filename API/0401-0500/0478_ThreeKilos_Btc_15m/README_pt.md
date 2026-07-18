# Estratégia Three Kilos BTC 15m
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Three Kilos BTC 15m combina três Médias Móveis Exponenciais Triplas (TEMA) com um filtro Supertrend. Uma posição comprada é aberta quando a TEMA média cruza acima da TEMA curta, permanece acima da TEMA lenta e o Supertrend indica uma tendência de alta. Uma posição vendida é aberta quando a TEMA curta cruza acima da TEMA média, permanece abaixo da TEMA lenta e o Supertrend mostra uma tendência de baixa. Um take profit e stop loss de porcentagem fixa gerenciam o risco.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: TEMA2 cruza acima de TEMA1, TEMA2 > TEMA3, tendência de alta no Supertrend.
  - **Vendido**: TEMA1 cruza acima de TEMA2, TEMA2 < TEMA3, tendência de baixa no Supertrend.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Take profit ou stop loss.
- **Stops**: Take profit de 1% e stop loss de 1%.
- **Valores padrão**:
  - `ShortPeriod` = 30
  - `LongPeriod` = 50
  - `Long2Period` = 140
  - `AtrLength` = 10
  - `Multiplier` = 2
  - `TakeProfit` = 1%
  - `StopLoss` = 1%
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: TEMA, Supertrend, ATR
  - Stops: Take profit e stop loss
  - Complexidade: Moderado
  - Período: 15m
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
