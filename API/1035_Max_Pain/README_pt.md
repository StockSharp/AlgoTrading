# Estratégia Max Pain
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre posições compradas quando tanto o volume quanto o movimento de preço excedem limites configuráveis e o índice VIX permanece abaixo de um nível especificado. Um stop-loss baseado em volatilidade é definido na entrada e a posição é fechada após um número fixo de períodos.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: volume maior que o volume médio × `VolumeMultiplier` e variação de preço maior que o fechamento anterior × `PriceChangeMultiplier` com VIX abaixo de `VixThreshold`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Stop-loss em `StopLossMultiplier` × volatilidade abaixo do preço de entrada.
  - Fechar posição após `HoldPeriods` barras.
- **Stops**: Sim.
- **Valores padrão**:
  - `LookbackPeriod` = 70.
  - `VolumeMultiplier` = 1.
  - `PriceChangeMultiplier` = 0.029.
  - `StopLossMultiplier` = 2.4.
  - `VixThreshold` = 44.
  - `HoldPeriods` = 8.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
  - `VixCandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Somente comprado
  - Indicadores: Volume, ação do preço, volatilidade
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
