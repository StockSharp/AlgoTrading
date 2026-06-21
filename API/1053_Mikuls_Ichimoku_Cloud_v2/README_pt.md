# Estratégia Mikul's Ichimoku Cloud v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento usando Ichimoku Cloud com filtro de média móvel opcional. As posições são gerenciadas por um trailing stop (ATR, percentual ou regras Ichimoku) e take-profit opcional.

## Detalhes

- **Critérios de entrada**: Tenkan-sen cruzando acima do Kijun-sen com o preço acima da nuvem, ou um forte rompimento acima de uma nuvem verde.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: Trailing stop ou reversão Ichimoku, take-profit opcional.
- **Stops**: Trailing.
- **Valores padrão**:
  - `TrailSource` = `LowsHighs`
  - `TrailMethod` = `Atr`
  - `TrailPercent` = 10
  - `SwingLookback` = 7
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1
  - `AddIchiExit` = false
  - `UseTakeProfit` = false
  - `TakeProfitPercent` = 25
  - `UseMaFilter` = false
  - `MaType` = `Ema`
  - `MaLength` = 200
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouBPeriod` = 52
  - `Displacement` = 26
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Comprado
  - Indicadores: Ichimoku, ATR
  - Stops: Trailing
  - Complexidade: Médio
  - Período: Intradiário (1h)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
