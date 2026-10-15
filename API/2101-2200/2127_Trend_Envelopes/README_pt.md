# Estratégia de Envelopes de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia seguidora de tendência construída sobre o indicador TrendEnvelopes. Combina uma EMA com bandas baseadas no ATR para detectar rompimentos.
Posições compradas são abertas quando o preço rompe acima da banda superior e um sinal de compra aparece. Posições vendidas são abertas em rompimentos abaixo da banda inferior com um sinal de venda. Bandas opostas acionam o encerramento das posições.

## Detalhes

- **Critérios de entrada**:
  - Comprado: preço fecha acima do envelope superior e gera um sinal de compra
  - Vendido: preço fecha abaixo do envelope inferior e gera um sinal de venda
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal de tendência oposto
- **Stops**: Sim (take profit e stop loss)
- **Valores padrão**:
  - `MaPeriod` = 14
  - `Deviation` = 0.2m
  - `AtrPeriod` = 15
  - `AtrSensitivity` = 0.5m
  - `TakeProfit` = 2000 pontos
  - `StopLoss` = 1000 pontos
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: 4h
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
