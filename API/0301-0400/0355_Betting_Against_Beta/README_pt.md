# Estratégia de Aposta Contra o Beta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Betting Against Beta** compra os ativos de menor beta e vende a descoberto os de maior beta. Os betas são
calculados em relação a um índice de referência em uma janela deslizante e o portfólio é rebalanceado no primeiro dia de negociação de cada
mês.

## Detalhes
- **Critérios de entrada**: classificar o universo por beta em relação ao índice de referência; comprado no decil mais baixo, vendido no mais alto.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Posições ajustadas no próximo rebalanceamento mensal.
- **Stops**: Sem lógica de stop explícita.
- **Valores padrão**:
  - `WindowDays = 252`
  - `Deciles = 10`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `MinTradeUsd = 100`
- **Filtros**:
  - Categoria: Fator
  - Direção: Ambos
  - Indicadores: Estatístico
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
