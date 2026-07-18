# Estratégia de Rompimento (BreakThrough)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia BreakThrough executa operações quando o preço cruza níveis de linha de tendência definidos pelo usuário.
Dois níveis principais são usados:
- **Buy Line** – nível de preço para acionar uma posição comprada.
- **Sell Line** – nível de preço para acionar uma posição vendida.

Quando uma linha é cruzada pelo lado oposto, a estratégia entra no mercado nessa direção.
Linhas adicionais opcionais permitem fechar uma posição quando o preço toca um nível específico.
As distâncias de stop-loss, take-profit e trailing stop protetores são medidas em pips a partir do preço de entrada.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o preço cruza acima ou abaixo da Buy Line dependendo de sua posição inicial.
  - **Vendido**: o preço cruza acima ou abaixo da Sell Line dependendo de sua posição inicial.
- **Comprado/Vendido**: ambos os lados.
- **Critérios de saída**:
  - O preço atinge uma linha opcional de take-profit ou stop-loss.
  - O preço alcança a distância de take-profit ou stop-loss em pips.
  - O trailing stop é acionado.
- **Stops**: sim, usando `StopLossPips`, `TakeProfitPips` e `TrailingStopPips`.
- **Valores padrão**:
  - `BuyLinePrice` = 0 (desativado)
  - `SellLinePrice` = 0 (desativado)
  - `TakeProfitPips` = 100
  - `StopLossPips` = 30
  - `TrailingStopPips` = 20
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Simples
  - Período: Qualquer (padrão 1 minuto)
  - Nível de risco: Médio
