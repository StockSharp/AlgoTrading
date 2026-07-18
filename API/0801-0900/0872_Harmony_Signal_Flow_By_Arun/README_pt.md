# Estratégia Harmony Signal Flow By Arun
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Harmony Signal Flow By Arun usa um RSI de período curto para capturar reversões com níveis fixos de stop-loss e alvo. A estratégia vai comprada quando o RSI cruza acima do limiar inferior e vendida quando cruza abaixo do limiar superior. As posições são fechadas por stop, alvo ou às 15:25 de cada dia.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: RSI cruza acima de `LowerThreshold`.
  - **Vendido**: RSI cruza abaixo de `UpperThreshold`.
- **Critérios de saída**: Stop-loss ou alvo atingido, ou fechamento às 15:25.
- **Stops**: Stop-loss e alvo fixos.
- **Valores padrão**:
  - `RsiPeriod` = 5
  - `LowerThreshold` = 30
  - `UpperThreshold` = 70
  - `BuyStopLoss` = 100
  - `BuyTarget` = 150
  - `SellStopLoss` = 100
  - `SellTarget` = 150
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado e Vendido
  - Indicadores: RSI
  - Complexidade: Baixo
  - Nível de risco: Médio
