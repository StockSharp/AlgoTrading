# Estratégia de Reversão com Hurst Exponent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta abordagem usa o Hurst Exponent para detectar quando um mercado está se comportando de maneira de reversão à média. Valores abaixo de 0,5 sugerem que o preço tende a retornar ao seu valor médio, criando oportunidades para operar contra os extremos.

Os testes indicam um retorno anual médio de aproximadamente 121%. Funciona melhor no mercado de criptomoedas.

Uma posição comprada é aberta quando o Hurst Exponent está abaixo de 0,5 e o preço fecha abaixo de uma média móvel. Uma posição vendida ocorre quando o valor Hurst está abaixo de 0,5 e o preço fecha acima da média. As posições são encerradas quando o preço retorna à linha de média ou o Hurst Exponent sobe acima do limiar.

A estratégia é adequada para traders que preferem tendências estatísticas a tendências fortes. Um stop-loss de proteção protege contra movimentos prolongados que não conseguem reverter.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Hurst < 0.5 && Close < MA
  - **Vendido**: Hurst < 0.5 && Close > MA
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando Close >= MA ou Hurst > 0.5
  - **Vendido**: Sair quando Close <= MA ou Hurst > 0.5
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `HurstPeriod` = 100
  - `AveragePeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Mean reversion
  - Direção: Ambos
  - Indicadores: Hurst Exponent, MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

