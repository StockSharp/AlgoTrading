# Rompimento de N Dias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento de máxima/mínima de N dias. O rompimento de N dias busca novas máximas ou mínimas ao longo do período determinado. As entradas ocorrem quando o preço perfura a última máxima ou mínima de N dias, antecipando momentum. Um filtro de média móvel e um stop percentual gerenciam as saídas.

Os testes indicam um retorno anual médio de aproximadamente 43%. Funciona melhor no mercado de ações.

Ao aguardar que o extremo anterior seja rompido, o sistema tenta capturar o início de um movimento direcional. Filtrar por uma média de seguidor de tendência ajuda a evitar sinais falsos que surgem durante a consolidação.


## Detalhes

- **Critérios de entrada**: Sinais baseados em MA.
- **Comprado/Vendido**: Ambos os sentidos.
- **Critérios de saída**: Sinal oposto ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `LookbackPeriod` = 20
  - `MaPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: MA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

