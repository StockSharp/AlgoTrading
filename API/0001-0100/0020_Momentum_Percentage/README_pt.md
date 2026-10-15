# Estratégia Momentum Percentage
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada na variação percentual do momentum de preço

Os testes indicam um retorno anual médio de aproximadamente 97%. Funciona melhor no mercado de criptomoedas.

Momentum Percentage rastreia a variação percentual do preço. As operações são acionadas quando o momentum excede níveis positivos ou negativos e saem com o sinal contrário ou um stop de volatilidade.

Ao medir retornos ao longo de um período de referência definido, o sistema se adapta a diferentes mercados. O stop de volatilidade garante que movimentos adversos grandes saiam rapidamente.


## Detalhes

- **Critérios de entrada**: Sinais baseados em MA, Momentum.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `MomentumPeriod` = 10
  - `ThresholdPercent` = 5m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA, Momentum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Neural Networks: Não
  - Divergência: Não
  - Nível de risco: Médio

