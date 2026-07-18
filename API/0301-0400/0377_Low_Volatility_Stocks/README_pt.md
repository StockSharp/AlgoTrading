# Estratégia de Ações de Baixa Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este fator defensivo de renda variável busca a "anomalia de baixa volatilidade" — a observação de que ações com movimentos de preço mais calmos frequentemente entregam retornos ajustados ao risco superiores. A volatilidade é calculada como o desvio padrão dos retornos diários ao longo de uma janela retroativa (60 dias úteis por padrão).

No primeiro dia útil de cada mês, o universo é classificado pela volatilidade realizada. A estratégia vai comprada no decil de menor volatilidade e vendida no decil de maior volatilidade, alocando pesos iguais em dólar dentro de cada grupo. As posições são mantidas até o próximo rebalanceamento mensal e nenhum stop explícito é utilizado.

Testes retrospectivos mostram uma curva de capital mais suave e menores drawdowns do que o mercado amplo, tornando a abordagem atraente para investidores que buscam exposição à renda variável com risco reduzido.

## Detalhes

- **Critérios de entrada**: Ordenação mensal por volatilidade retroativa; comprado no decil mais baixo,
  vendido no decil mais alto
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Próximo rebalanceamento mensal
- **Stops**: Não
- **Valores padrão**:
  - `VolWindowDays` = 60
  - `Deciles` = 10
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Ambos
  - Indicadores: Desvio padrão
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
