# Estratégia Martin - Saída Sem Perdas V3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de média martingala adiciona a uma posição comprada sempre que o preço cai uma porcentagem configurada a partir da primeira entrada. Cada nova ordem aumenta o valor em dinheiro por um multiplicador e ajusta o preço médio. A posição é fechada quando a máxima do candle atinge o preço médio mais a porcentagem de take profit, garantindo saídas apenas com lucro.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Flat` → comprar por `Initial Cash`
  - **Adicionar**: `Price <= EntryPrice * (1 - PriceStep% * orderCount)` && `orderCount < MaxOrders`
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**:
  - `High >= AvgPrice * (1 + TakeProfit%)`
- **Stops**: Não
- **Valores padrão**:
  - `Initial Cash` = 100
  - `Max Orders` = 20
  - `Price Step %` = 1.5
  - `Take Profit %` = 1
  - `Increase Factor` = 1.05
- **Filtros**:
  - Categoria: Média para baixo
  - Direção: Somente comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
