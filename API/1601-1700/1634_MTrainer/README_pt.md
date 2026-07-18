# Estratégia MTrainer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia MTrainer replica o script MTrainer do MT4. Ela abre uma posição quando o preço atinge uma linha de entrada predefinida e a gerencia com stop-loss, take-profit e linhas opcionais de fechamento parcial. A estratégia foi projetada para prática manual no testador visual.

## Detalhes

- **Critérios de entrada**: o preço cruza a linha de entrada
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: stop loss, take profit ou fechamento parcial
- **Stops**: Sim
- **Valores padrão**:
  - `EntryPrice` = 0
  - `TakeProfitPrice` = 0
  - `StopLossPrice` = 0
  - `PartialClosePercent` = 0
  - `PartialClosePrice` = 0
  - `Volume` = 1
- **Filtros**:
  - Categoria: Utilitário
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
