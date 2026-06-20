# Bot para Mercado Spot - Estratégia de Grade Personalizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Bot para Mercado Spot - Grade Personalizada compra uma posição inicial e adiciona novas ordens quando o preço cai um percentual especificado abaixo do último ponto de entrada. Fecha todas as posições quando o preço sobe acima do preço médio de entrada pelo alvo de lucro.

## Detalhes

- **Critérios de entrada**:
  - Comprar no momento de início.
  - Comprar quantidade adicional quando o preço cai `NextEntryPercent`% abaixo do último ponto de entrada.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Fechar todas as posições quando o preço exceder o preço médio de entrada em `ProfitPercent`% e a posição aberta for lucrativa.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `OrderValue` = 10
  - `MinAmountMovement` = 0.00001
  - `Rounding` = 5
  - `NextEntryPercent` = 0.5
  - `ProfitPercent` = 2
- **Filtros**:
  - Categoria: Grid trading
  - Direção: Comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
