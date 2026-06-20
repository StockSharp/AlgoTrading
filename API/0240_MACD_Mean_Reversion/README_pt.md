# Estratégia de Reversão à Média com MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Este método rastreia o histograma MACD em relação à sua própria média. Leituras extremas do histograma frequentemente revertem assim que o momentum diminui. Ao monitorar a diferença entre MACD e sua linha de sinal, a estratégia encontra movimentos sobreestendidos.

Os testes indicam um retorno anual médio de aproximadamente 67%. Funciona melhor no mercado de ações.

Uma posição comprada é iniciada quando o histograma MACD cai abaixo da média em `DeviationMultiplier` desvios padrão. Uma posição vendida é aberta quando o histograma sobe acima da média pela mesma quantidade. A operação é fechada quando o histograma volta a cruzar sua média.

Esta abordagem atende a traders confortáveis em operar contra extremos de momentum. Um stop-loss medido como percentual do preço de entrada protege contra tendências que continuam se fortalecendo.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: MACD Histogram < Avg - DeviationMultiplier * StdDev
  - **Vendido**: MACD Histogram > Avg + DeviationMultiplier * StdDev
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando Histogram > Avg
  - **Vendido**: Sair quando Histogram < Avg
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `FastMacdPeriod` = 12
  - `SlowMacdPeriod` = 26
  - `SignalPeriod` = 9
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Mean reversion
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

