# Estratégia Keltner Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que opera em reversão à média usando Canais de Keltner

Os testes indicam um retorno anual médio de aproximadamente 130%. Funciona melhor no mercado de ações.

Keltner Reversion opera contra impulsos fora do Canal de Keltner. As entradas apostam em um retorno em direção à banda média, fechando trades quando o preço volta a entrar no canal ou o stop é atingido.

A largura do canal se expande e contrai com a volatilidade, permitindo que o sistema capture movimentos extremos enquanto dá espaço para os trades se desenvolverem. Os stops são tipicamente baseados em múltiplos de ATR.


## Detalhes

- **Critérios de entrada**: Sinais baseados em RSI, ATR, Keltner.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `StopLossAtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: RSI, ATR, Keltner
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

