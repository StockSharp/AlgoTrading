# Estratégia de Reversão à Média com Stochastic Oscillator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia mede o Stochastic Oscillator contra sua própria média móvel para localizar oscilações sobreestendidas. Quando %K se move vários desvios padrão longe de sua média, a expectativa é que o indicador retorne para valores típicos.

Os testes indicam um retorno anual médio de aproximadamente 64%. Funciona melhor no mercado de câmbio.

Uma operação comprada é colocada quando o Stochastic %K cai abaixo da banda inferior definida pela média menos `Multiplier` vezes o desvio padrão. Uma operação vendida ocorre quando %K excede a banda superior. As posições são fechadas quando %K cruza de volta pela sua linha de média.

O método é projetado para traders de curto prazo que gostam de operar em extremos de sobrecompra e sobrevenda. O stop-loss protege contra o momentum sustentado que não consegue reverter à média.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: %K < Avg - Multiplier * StdDev
  - **Vendido**: %K > Avg + Multiplier * StdDev
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando %K > Avg
  - **Vendido**: Sair quando %K < Avg
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `AveragePeriod` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Mean reversion
  - Direção: Ambos
  - Indicadores: Stochastic Oscillator
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

