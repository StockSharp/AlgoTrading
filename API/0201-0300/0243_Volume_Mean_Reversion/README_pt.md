# Estratégia de Reversão à Média de Volume
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este sistema busca volume de negociação incomumente alto ou baixo em relação à sua média histórica. Picos de volume significativos frequentemente revertem à medida que a atividade se normaliza, oferecendo possíveis operações contra o movimento.

Os testes indicam um retorno anual médio de cerca de 76%. Funciona melhor no mercado forex.

Uma entrada comprada é realizada quando o volume cai abaixo da média menos `DeviationMultiplier` vezes o desvio padrão e o preço está abaixo da média móvil. Uma entrada vendida ocorre quando o volume sobe acima da banda superior com o preço acima da média. As operações são encerradas assim que o volume retorna em direção ao seu nível médio.

A estratégia beneficia traders que observam o esgotamento após picos de volume. Um stop-loss percentual protege contra cenários onde o volume continua se expandindo na mesma direção.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Volume < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Vendido**: Volume > Avg + DeviationMultiplier * StdDev && Close > MA
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando volume > Avg
  - **Vendido**: Sair quando volume < Avg
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2m
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Volume
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
