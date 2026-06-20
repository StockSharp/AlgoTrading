# Elder Impulse
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no Sistema de Impulso de Elder

Os testes indicam um retorno anual médio de aproximadamente 106%. Funciona melhor no mercado de ações.

Elder Impulse combina a direção da EMA com a cor do histograma do MACD. Barras verdes acima da EMA impulsionam posições compradas, barras vermelhas abaixo impulsionam vendidas, e barras neutras sinalizam saídas.

Ao combinar direção de tendência e momentum, essa abordagem mantém os traders no lado certo dos movimentos fortes. As saídas são diretas, dependendo da mudança de cor do histograma ou da inversão da inclinação da EMA.


## Detalhes

- **Critérios de entrada**: Sinais baseados em MACD.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `EmaPeriod` = 13
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MACD
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

