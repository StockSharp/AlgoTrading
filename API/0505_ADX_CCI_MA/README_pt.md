# ADX CCI MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia combina ADX, CCI e uma média móvel configurável para operar tendências fortes.

O sistema compra quando +DI cruza acima de -DI, CCI > 100 e ADX excede o limite (opcionalmente fechamento acima da MA). Vende a descoberto quando -DI cruza acima de +DI, CCI < -100 e ADX excede o limite (fechamento abaixo da MA).

Inclui stop-loss e take-profit baseados em percentual mais gerenciamento de risco opcional com MA que sai após várias velas fechando contra a média móvel.

## Detalhes

- **Critérios de entrada**: Cruzamento de +DI/-DI com extremo de CCI e ADX > `AdxThreshold`, fechamento vs MA opcional.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss ou take-profit atingido, gerenciamento de risco opcional com MA.
- **Stops**: Sim, take profit e stop loss.
- **Valores padrão**:
  - `EnableLong` = true
  - `EnableShort` = true
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CciPeriod` = 15
  - `AdxLength` = 10
  - `AdxThreshold` = 20m
  - `UseMaTrend` = true
  - `MaType` = MovingAverageTypeEnum.Simple
  - `MaLength` = 200
  - `UseMaRiskManagement` = false
  - `MaRiskExitCandles` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: ADX, CCI, MA
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
