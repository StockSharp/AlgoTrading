# Estratégia de Momentum, Reversão e Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de fator composto combina três sinais: momentum de longo prazo,
reversão de curto prazo e baixa volatilidade. A cada mês é calculada uma pontuação
para cada ativo usando o momentum de 12 meses, o inverso dos retornos de um mês e a
volatilidade dos últimos 60 dias. Os pesos ajustáveis `WM`, `WR` e `WV` controlam
a contribuição de cada componente.

No primeiro dia de negociação de cada mês, os ativos são classificados pela pontuação
composta. O decil mais alto é comprado e o decil mais baixo é vendido a descoberto com
pesos iguais em dólares. As posições são mantidas até o próximo rebalanceamento e não
são empregadas regras explícitas de stop-loss.

Ao combinar seguidor de tendência, reversão à média e aversão ao risco, a estratégia
busca retornos diversificados em diferentes regimes de mercado.

## Detalhes

- **Critérios de entrada**: Classificação mensal por combinação ponderada de momentum,
  reversão e volatilidade; comprado no decil superior, vendido no decil inferior
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Próximo rebalanceamento mensal
- **Stops**: Não
- **Valores padrão**:
  - `Lookback12` = 252
  - `Lookback1` = 21
  - `VolWindow` = 60
  - `WM` = 1.0
  - `WR` = 1.0
  - `WV` = 1.0
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Multi-fator
  - Direção: Ambos
  - Indicadores: Momentum, reversão, volatilidade
  - Stops: Não
  - Complexidade: Avançado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
