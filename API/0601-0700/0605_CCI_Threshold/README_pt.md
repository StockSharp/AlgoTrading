# Estratégia de Limiar CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que compra quando o CCI cai abaixo de um limiar e sai quando o preço de fechamento supera o fechamento anterior.
Stop loss e take profit opcionais em pontos absolutos.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `CCI < BuyThreshold`
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**:
  - `ClosePrice > previous ClosePrice`
- **Stops**: Opcional via `UseStopLoss` e `UseTakeProfit`
- **Valores padrão**:
  - `LookbackPeriod` = 12
  - `BuyThreshold` = -90
  - `StopLossPoints` = 100m
  - `TakeProfitPoints` = 150m
  - `UseStopLoss` = false
  - `UseTakeProfit` = false
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: CCI
  - Stops: Opcional
  - Complexidade: Baixo
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
