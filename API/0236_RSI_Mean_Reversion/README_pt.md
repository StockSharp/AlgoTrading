# Estratégia de Reversão à Média com RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia rastreia o índice de força relativa e mede sua distância de um nível médio. Quando o RSI se desvia em mais de um múltiplo de seu desvio padrão recente, o algoritmo espera um retorno à média.

Os testes indicam um retorno anual médio de aproximadamente 61%. Funciona melhor no mercado de criptomoedas.

Uma operação comprada é aberta quando o RSI cai abaixo da banda inferior definida pela média menos `Multiplier` vezes o desvio padrão. Uma operação vendida é tomada quando o RSI sobe acima da banda superior. As saídas ocorrem quando o RSI retorna à sua média móvel.

O método é adequado para traders que buscam sinais objetivos de sobrecompra e sobrevenda. Usar uma banda baseada em volatilidade adapta os limiares às condições atuais do mercado, enquanto um stop-loss mantém as perdas limitadas.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: RSI < Avg - Multiplier * StdDev
  - **Vendido**: RSI > Avg + Multiplier * StdDev
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando RSI > Avg
  - **Vendido**: Sair quando RSI < Avg
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Mean reversion
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

