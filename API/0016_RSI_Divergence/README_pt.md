# RSI Divergence
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada em divergência do RSI

Os testes indicam um retorno anual médio de aproximadamente 85%. Funciona melhor no mercado de criptomoedas.

RSI Divergence procura extremos de preço não confirmados pelo oscilador RSI. Uma divergência de alta leva a uma compra e uma divergência de baixa provoca uma venda. A operação dura até que o RSI reverta ou um stop seja ativado.

Configurações de divergência frequentemente surgem perto do fim de tendências longas. Ao comparar o comportamento do oscilador com a ação do preço, a estratégia tenta capturar reversões antecipadas com risco controlado.


## Detalhes

- **Critérios de entrada**: Sinais baseados em RSI.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Neural Networks: Não
  - Divergência: Sim
  - Nível de risco: Médio

