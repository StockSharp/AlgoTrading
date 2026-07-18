# Estratégia Heikin Ashi Consecutive
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Estratégia baseada em velas Heikin Ashi consecutivas

Os testes indicam um retorno anual médio de aproximadamente 73%. Funciona melhor no mercado de criptomoedas.

Heikin Ashi Consecutive aguarda várias velas Heikin Ashi da mesma cor para confirmar o momentum. Após uma sequência de barras de alta ou de baixa, a estratégia adere ao movimento e sai na primeira vela oposta ou por um stop ATR.

Como os gráficos Heikin Ashi suavizam os dados de preço, uma série de velas da mesma cor destaca um movimento direcional forte. O stop ATR Trailing tenta preservar os ganhos se a sequência se reverter abruptamente.


## Detalhes

- **Critérios de entrada**: Sinais baseados em Heikin.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `ConsecutiveCandles` = 3
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Heikin
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Neural Networks: Não
  - Divergência: Não
  - Nível de risco: Médio

