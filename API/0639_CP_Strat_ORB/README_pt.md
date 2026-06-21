# CP Strat ORB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera rompimentos da faixa de abertura de Nova York (9:30-9:45) com um reteste. Entra comprado após o preço romper acima da máxima da faixa e fechar novamente acima dela, e entra vendido após o preço romper abaixo da mínima da faixa e fechar novamente abaixo. As saídas utilizam níveis fixos de stop-loss e take-profit.

## Detalhes

- **Critérios de entrada**: Rompimento da faixa de abertura de NY seguido de um reteste e fechamento além do limite da faixa.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Take-profit ou stop-loss fixo.
- **Stops**: Sim.
- **Valores padrão**:
  - `MinRangePoints` = 60m
  - `StopPoints` = 20m
  - `TakePoints` = 60m
  - `MaxTradesPerSession` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
