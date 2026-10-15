# Backtest da Estratégia LANZ 4.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Backtest da Estratégia LANZ 4.0 é uma estratégia de rompimento que usa pivôs de swing para detectar mudanças de tendência. Quando o preço rompe acima do último máximo de pivô, entra comprado; quando o preço rompe abaixo do último mínimo de pivô, entra vendido. O tamanho da posição é calculado a partir do percentual de risco e do valor em pips, com stop-loss abaixo/acima do último swing mais buffer e take-profit pela relação risco-retorno.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: O preço cruza acima do último máximo de pivô.
  - **Vendido**: O preço cruza abaixo do último mínimo de pivô.
- **Critérios de saída**: Stop-loss ou take-profit.
- **Stops**: Máximo/mínimo de swing recente com buffer.
- **Valores padrão**:
  - `SwingLength` = 180
  - `SlBufferPoints` = 50
  - `RiskReward` = 1
  - `RiskPercent` = 1
  - `PipValueUsd` = 10
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Comprado e Vendido
  - Indicadores: Highest, Lowest
  - Complexidade: Moderado
  - Nível de risco: Médio
