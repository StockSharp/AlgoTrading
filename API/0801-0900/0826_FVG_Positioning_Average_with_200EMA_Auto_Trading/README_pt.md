# Estratégia de Média de Posicionamento FVG com 200EMA de Auto Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia calcula a média dos níveis de fair value gaps (FVG) de alta e de baixa e os combina com uma EMA de 200 períodos. Uma operação é aberta quando o preço cruza essas médias na direção da tendência.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço cruza acima da média dos FVG de baixa e todas as médias estão acima da EMA.
  - **Vendido**: O preço cruza abaixo da média dos FVG de alta e todas as médias estão abaixo da EMA.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Stop-loss na mínima/máxima recente.
  - Take profit na relação risco-retorno.
- **Stops**: Sim.
- **Valores padrão**:
  - `FvgLookback` = 30
  - `AtrMultiplier` = 0.25
  - `LookbackPeriod` = 20
  - `EmaPeriod` = 200
  - `RiskReward` = 1.5
- **Filtros**:
  - Categoria: Price action
  - Direção: Ambos
  - Indicadores: ATR, EMA, SMA, Highest, Lowest
  - Stops: Sim
  - Complexidade: Médio
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
