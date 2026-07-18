# Estratégia de Ações por Fator de Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta abordagem sistemática aproveita o clássico fator de momentum de 12-1 meses em ações. No final de cada mês, as ações são classificadas pelo seu desempenho nos doze meses anteriores, ignorando o mês mais recente para contornar reversões de curto prazo. Os ativos no quintil mais alto são comprados e os do quintil mais baixo são vendidos a descoberto, formando um spread neutro ao mercado.

O rebalanceamento ocorre no primeiro dia útil de cada mês. As posições têm peso igual e permanecem abertas até o próximo rebalanceamento; nenhum stop explícito é utilizado.

Extensa pesquisa acadêmica e setorial mostra que o momentum entrega retornos excessivos persistentes e oferece diversificação valiosa quando combinado com outros fatores.

## Detalhes

- **Critérios de entrada**: Classificação mensal por momentum 12-1; comprado no quintil superior,
  vendido no quintil inferior
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Próximo rebalanceamento mensal
- **Stops**: Não
- **Valores padrão**:
  - `LookbackDays` = 252
  - `SkipDays` = 21
  - `Quintile` = 5
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Variação de preço
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
