# Estratégia de Reversão à Média com ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Aqui o Average Directional Index (ADX) mede a força geral da tendência. Quando o ADX está baixo, o mercado carece de direção e os preços tendem a oscilar em torno de um valor médio. Esta estratégia explora esse comportamento negociando os desvios do ADX de sua média móvel.

Os testes indicam um retorno anual médio de aproximadamente 70%. Funciona melhor no mercado de ações.

Uma operação comprada é iniciada quando o ADX cai abaixo da média menos `DeviationMultiplier` vezes o desvio padrão e o preço está abaixo da média móvel. Uma operação vendida é aberta quando o ADX dispara acima da banda superior e o preço está acima da média. As posições são fechadas quando o ADX reverte em direção à sua média.

Este sistema atrai traders que buscam oportunidades em ambientes de baixa tendência. O stop-loss evita que pequenas operações de reversão à média se tornem grandes perdas se uma nova tendência surgir.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: ADX < Avg - DeviationMultiplier * StdDev && Close < MA
  - **Vendido**: ADX > Avg + DeviationMultiplier * StdDev && Close > MA
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando ADX > Avg
  - **Vendido**: Sair quando ADX < Avg
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `AdxPeriod` = 14
  - `AveragePeriod` = 20
  - `DeviationMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Mean reversion
  - Direção: Ambos
  - Indicadores: ADX
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

