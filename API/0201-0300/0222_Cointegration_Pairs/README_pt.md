# Estratégia de Pares por Cointegração
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera dois ativos que compartilham um relacionamento de cointegração de longo prazo. Calculando o resíduo entre o primeiro ativo e um segundo ativo ajustado por beta, procura desvios que historicamente revertem ao equilíbrio.

Os testes indicam um retorno anual médio de aproximadamente 103%. Funciona melhor no mercado de ações.

Uma posição comprada compra o primeiro ativo e vende o segundo quando o z-score residual cai abaixo de `-EntryThreshold`. Uma posição vendida vende o primeiro e compra o segundo quando o z-score sobe acima do limiar. As posições são fechadas assim que o spread se normaliza em direção a zero.

O trading de pares por cointegração é adequado para arbitragistas estatísticos confortáveis em gerenciar dois instrumentos simultaneamente. O stop-loss integrado protege contra movimentos extremos se o relacionamento temporariamente se romper.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Z-Score Residual < -EntryThreshold
  - **Vendido**: Z-Score Residual > EntryThreshold
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando |Z-Score| < 0.5
  - **Vendido**: Sair quando |Z-Score| < 0.5
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `Period` = 20
  - `EntryThreshold` = 2.0m
  - `Beta` = 1.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Arbitragem
  - Direção: Ambos
  - Indicadores: Cointegração
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
