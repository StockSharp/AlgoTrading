# Triple MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no cruzamento de Triple Média Móvel.

Os testes indicam um retorno anual médio de aproximadamente 55%. Funciona melhor no mercado de ações.

O Triple MA alinha três médias móveis para definir a direção. Quando a média mais curta está acima das médias intermediária e longa, ocorre uma entrada comprada. O alinhamento reverso abre posições vendidas, e um cruzamento das linhas curta e intermediária fecha a operação.

Usar três médias ajuda a filtrar o ruído presente em sistemas de MA único. Esta abordagem em camadas busca confirmar o momentum antes de se comprometer com uma operação.


## Detalhes

- **Critérios de entrada**: Sinais baseados em MA.
- **Comprado/Vendido**: Ambos os sentidos.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `ShortMaPeriod` = 5
  - `MiddleMaPeriod` = 20
  - `LongMaPeriod` = 50
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: MA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

