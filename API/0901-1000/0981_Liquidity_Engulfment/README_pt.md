# Estratégia de Engolfamento de Liquidez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia detecta padrões de engolfamento altista e baixista que ocorrem após o preço tocar máximas ou mínimas recentes de liquidez. As operações são filtradas por modo e incluem stop loss fixo e take profit opcional definidos em pips.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Engolfamento altista após toque na liquidez inferior.
  - **Vendido**: Engolfamento baixista após toque na liquidez superior.
- **Critérios de saída**: Sinal oposto, stop loss ou take profit.
- **Comprado/Vendido**: Configurável (ambos por padrão).
- **Indicadores**: Highest, Lowest.
- **Stops**: `StopLossPips` e `TakeProfitPips` opcional.
- **Valores padrão**:
  - `CandleType` = 1 minuto
  - `UpperLookback` = 10
  - `LowerLookback` = 10
  - `StopLossPips` = 10
  - `TakeProfitPips` = 20
  - `Mode` = Both
