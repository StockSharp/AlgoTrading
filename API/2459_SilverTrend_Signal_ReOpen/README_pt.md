# Estratégia SilverTrend Signal ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no indicador SilverTrend com reabertura opcional. Abre uma posição quando o indicador muda de direção e adiciona posições adicionais cada vez que o preço avança um passo definido a favor da operação. As posições podem ser fechadas em sinais opostos ou quando os níveis de stop loss / take-profit são atingidos.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o indicador SilverTrend muda de tendência de baixa para tendência de alta
  - Vendido: o indicador SilverTrend muda de tendência de alta para tendência de baixa
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Opcionalmente fechar em sinais SilverTrend opostos
  - Stop Loss ou Take Profit atingido
- **Stops**: Níveis de preço absolutos
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Ssp` = 9
  - `Risk` = 3
  - `PriceStep` = 300m
  - `PosTotal` = 10
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SilverTrend
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
