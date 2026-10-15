# Estratégia de Cruzamento EMA WMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia baseada no cruzamento entre a média móvel exponencial (EMA) e a média móvel ponderada (WMA) calculadas sobre os preços de abertura das velas.
Entra comprado quando a EMA cruza abaixo da WMA e vendido quando a EMA cruza acima da WMA.
O tamanho da posição é determinado pelo percentual de risco do patrimônio da conta.
A estratégia usa distâncias fixas de take-profit e stop loss definidas em ticks.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `EMA crosses below WMA`
  - Vendido: `EMA crosses above WMA`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Stop loss ou take-profit
- **Stops**: Sim
- **Valores padrão**:
  - `EmaPeriod` = 28
  - `WmaPeriod` = 8
  - `StopLossTicks` = 50
  - `TakeProfitTicks` = 50
  - `RiskPercent` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoria: Cruzamento de médias móveis
  - Direção: Ambos
  - Indicadores: EMA, WMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
