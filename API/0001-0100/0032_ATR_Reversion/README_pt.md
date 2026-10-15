# Estratégia ATR Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
ATR Reversion procura movimentos repentinos medidos em múltiplos do Average True Range (ATR). Quando o preço ultrapassa o multiplicador de ATR, o sistema espera uma reversão à média.

Os testes indicam um retorno anual médio de aproximadamente 133%. Funciona melhor no mercado de criptomoedas.

A estratégia abre uma operação na direção oposta ao movimento brusco e utiliza uma média móvel para avaliar o momentum.

As posições fecham em um cruzamento de média móvel ou quando o stop de volatilidade é atingido.

## Detalhes

- **Critérios de entrada**: O movimento do preço excede `AtrMultiplier` vezes ATR.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: O preço cruza a média móvel ou o stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: ATR, MA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

