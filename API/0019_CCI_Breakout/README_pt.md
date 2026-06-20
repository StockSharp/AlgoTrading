# CCI Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada em rompimento do CCI (Commodity Channel Index)

Os testes indicam um retorno anual médio de aproximadamente 94%. Funciona melhor no mercado de ações.

CCI Breakout utiliza o Commodity Channel Index para identificar movimentos poderosos. Surtos além dos limiares positivos ou negativos do CCI geram entradas. As saídas ocorrem quando o CCI recua em direção a zero ou se forma um sinal oposto.

Como o CCI mede o desvio de uma média móvel, leituras extremas implicam preços insustentáveis. Este sistema aguarda esses extremos e então tenta lucrar com o seguimento.


## Detalhes

- **Critérios de entrada**: Sinais baseados em CCI, Momentum.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `CciPeriod` = 20
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: CCI, Momentum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Neural Networks: Não
  - Divergência: Não
  - Nível de risco: Médio

