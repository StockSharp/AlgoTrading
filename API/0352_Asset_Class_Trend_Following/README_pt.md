# Estratégia de Seguidor de Tendência por Classe de Ativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia segue tendências em múltiplas classes de ativos. Aplica um filtro de média móvel simples a cada ativo do universo e rebalanceia o portfólio no primeiro dia de negociação de cada mês. As posições são abertas apenas quando o preço está acima da média móvel.

Os testes indicam um retorno anual médio de aproximadamente 15%. Tem melhor desempenho em portfólios de futuros diversificados.

No início de cada mês, os ativos negociados acima de sua SMA recebem uma alocação igual de capital. As posições são fechadas quando o preço cai abaixo da SMA ou quando o capital é redistribuído no próximo rebalanceamento.

## Detalhes

- **Critérios de entrada**: `Close > SMA`
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: `Close <= SMA` ou removido no rebalanceamento
- **Stops**: Nenhum; o capital é redistribuído mensalmente
- **Valores padrão**:
  - `SmaLength` = 210
  - `MinTradeUsd` = 50
  - `CandleType` = daily
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Somente comprado
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Longo prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
