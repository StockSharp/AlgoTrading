# Estratégia de Reversão à Média de Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta abordagem opera em torno das flutuações da volatilidade do mercado. Quando o ATR desvia significativamente de sua média móvel, sugere que a volatilidade tornou-se incomumente alta ou baixa e pode reverter.

Os testes indicam um retorno anual médio de cerca de 73%. Funciona melhor no mercado de criptomoedas.

A estratégia vai comprado quando o ATR cai abaixo da média menos `DeviationMultiplier` vezes o desvio padrão e o preço está abaixo da média móvel. Vai vendido quando o ATR supera a banda superior e o preço está acima da média. As posições são encerradas assim que o ATR retorna em direção ao seu nível médio.

Esses setups funcionam para traders que preferem operar contra extremos de volatilidade em vez da direção do preço. Um stop-loss protetor é usado caso a volatilidade continue se expandindo.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: ATR < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Vendido**: ATR > Avg + DeviationMultiplier * StdDev && Close > MA
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando ATR > Avg
  - **Vendido**: Sair quando ATR < Avg
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `AtrPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
