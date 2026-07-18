# Estratégia Grim Slash
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Grim Slash é uma estratégia simples de ação de preço que compra quando a mínima da vela atual toca o fechamento anterior e sai quando a máxima alcança a máxima anterior. O risco é gerenciado com take profit e stop loss de percentual fixo.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: A mínima atual toca ou cai abaixo do fechamento anterior.
- **Critérios de saída**: A máxima atual toca ou supera a máxima anterior.
- **Stops**: Take profit de 15%, stop loss de 5%.
- **Valores padrão**:
  - `TakeProfitPercent` = 15
  - `StopLossPercent` = 5
- **Filtros**:
  - Categoria: Reversão
  - Direção: Somente comprado
  - Indicadores: Nenhum
  - Complexidade: Baixo
  - Nível de risco: Médio
