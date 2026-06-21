# Estratégia Bober XM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Bober XM usa uma abordagem de canal duplo baseada em um cálculo personalizado de Keltner. As entradas por rompimento são confirmadas por uma Média Móvel Ponderada e a força geral da tendência pelo ADX. As saídas dependem do On-Balance Volume cruzando sua média móvel enquanto o ADX permanece forte.

Projetada para traders que buscam confirmação de momentum com saídas baseadas em volume.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Close > UpperBand && Close > WMA && ADX > Threshold`
  - **Vendido**: `Close < LowerBand && Close < WMA && ADX > Threshold`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - **Comprado**: `OBV < OBV_MA && ADX > Threshold`
  - **Vendido**: `OBV > OBV_MA && ADX > Threshold`
- **Stops**: Stop-loss percentual via `StopLossPercent`
- **Valores padrão**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 10
  - `KeltnerMultiplier` = 1.5m
  - `WmaPeriod` = 15
  - `ObvMaPeriod` = 22
  - `AdxPeriod` = 60
  - `AdxThreshold` = 35m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Keltner Channel, WMA, OBV, ADX
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
