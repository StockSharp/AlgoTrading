# Donky MA TP SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera cruzamentos de médias móveis com dois alvos de take-profit e um stop-loss. Entra comprado quando a SMA rápida cruza acima da SMA lenta e vendido quando cruza abaixo. Metade da posição é fechada no primeiro alvo e o restante no segundo alvo ou no stop-loss.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: SMA rápida cruza acima da SMA lenta.
  - **Vendido**: SMA rápida cruza abaixo da SMA lenta.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Dois níveis fixos de take-profit ou um stop-loss fixo.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastLength` = 10
  - `SlowLength` = 30
  - `TakeProfit1Pct` = 0.03m
  - `TakeProfit2Pct` = 0.06m
  - `StopLossPct` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
