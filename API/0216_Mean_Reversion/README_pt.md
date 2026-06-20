# Mean Reversion Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta abordagem estatística busca extremos de curto prazo no preço em relação à sua média recente. A estratégia usa uma média móvel para definir o valor justo e mede o desvio dessa média por meio de um cálculo de desvio padrão.

Os testes indicam um retorno anual médio de aproximadamente 85%. Funciona melhor no mercado de criptomoedas.

Os trades são abertos quando o preço empurra a uma distância definida da média. Uma queda abaixo da banda inferior aciona uma entrada comprada, antecipando um rebote em direção à média, enquanto um rally acima da banda superior provoca um short. Assim que o preço toca a média móvel novamente, qualquer posição aberta é fechada.

O método atrai traders com estilo contrário que desejam zonas de entrada e saída claramente definidas. Por se basear em bandas baseadas em volatilidade, se adapta a mercados mais tranquilos ou mais ativos, mantendo as perdas sob controle por meio de um stop-loss fixo.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Price < MA - k*StdDev (below lower band)
  - **Vendido**: Price > MA + k*StdDev (above upper band)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando o preço cruzar acima da média móvel
  - **Vendido**: Sair quando o preço cruzar abaixo da média móvel
- **Stops**: Sim.
- **Valores padrão**:
  - `MovingAveragePeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Mean Reversion
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

