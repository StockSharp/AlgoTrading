# Estratégia Ilan Dynamic HT
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de Martingale baseada em grade que abre posições com base em sinais RSI e expande a posição usando um intervalo de preços dinâmico. Cada operação adicional aumenta o volume por um multiplicador e compartilha o mesmo take profit e stop loss.

## Detalhes

- **Critérios de entrada**:
  - Comprado: RSI abaixo de `RsiMinimum`
  - Vendido: RSI acima de `RsiMaximum`
- **Comprado/Vendido**: Comprado e Vendido
- **Critérios de saída**:
  - Take profit ou stop loss comum é atingido
- **Stops**:
  - `TakeProfit` em pontos
  - `StopLoss` em pontos
- **Valores padrão**:
  - `LotExponent` = 1.4
  - `MaxTrades` = 10
  - `DynamicPips` = true
  - `DefaultPips` = 120
  - `Depth` = 24
  - `Del` = 3
  - `BaseVolume` = 0.1
  - `RsiPeriod` = 14
  - `RsiMinimum` = 30
  - `RsiMaximum` = 70
  - `TakeProfit` = 100
  - `StopLoss` = 500
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoria: Grade / Martingale
  - Direção: Comprado e Vendido
  - Indicadores: RSI, Highest, Lowest
  - Stops: Take Profit, Stop Loss
  - Complexidade: Avançado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
