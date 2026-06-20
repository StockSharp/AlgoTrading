# RSI Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada na reversão à média do RSI

Os testes indicam um retorno anual médio de aproximadamente 115%. Funciona melhor no mercado de ações.

RSI Reversion assume que o preço reverterá após atingir valores extremos do RSI. Quando o RSI cai abaixo do limiar inferior, compra; quando está acima do limiar superior, vende. As posições fecham à medida que o RSI retorna a níveis neutros.

Os extremos podem ser calibrados para se adequar a vários mercados. Usar filtros adicionais como a direção da tendência ajuda a evitar se opor a movimentos fortes cedo demais.


## Detalhes

- **Critérios de entrada**: Sinais baseados em RSI.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `OversoldThreshold` = 30m
  - `OverboughtThreshold` = 70m
  - `ExitLevel` = 50m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

