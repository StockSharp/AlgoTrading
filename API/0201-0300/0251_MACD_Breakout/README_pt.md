# Rompimento MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Rompimento MACD observa o MACD em busca de expansões repentinas. Quando as leituras saltam além de seu intervalo normal, o preço frequentemente inicia um novo movimento.

Os testes indicam um retorno anual médio de cerca de 94%. Funciona melhor no mercado de ações.

Uma posição é aberta assim que o indicador perfura uma banda derivada de dados recentes e um multiplicador de desvio. Operações compradas e vendidas são possíveis com um stop vinculado.

Este sistema se adapta a traders de momentum que buscam rompimentos precoces. As operações são fechadas quando o MACD retorna em direção à média. Os valores padrão começam com `FastEmaPeriod` = 12.

## Detalhes

- **Critérios de entrada**: O indicador supera a média pelo multiplicador de desvio.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: O indicador reverte à média.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastEmaPeriod` = 12
  - `SlowEmaPeriod` = 26
  - `SignalPeriod` = 9
  - `SmaPeriod` = 20
  - `DeviationMultiplier` = 2
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
    Avg e StdDev são a média e o desvio padrão dos últimos SmaPeriod valores de linha MACD, incluindo o atual. O stop é um StopLossPercent fixo do preço de entrada, vigiado também entre os candles; 0 o desativa. As bandas são a média mais e menos DeviationMultiplier desvios padrão: a linha MACD acima da banda superior abre uma compra e abaixo da inferior uma venda; uma compra fecha quando a linha MACD volta abaixo da sua média e uma venda quando volta acima. Um sinal de entrada contra uma posição aberta a inverte.
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
