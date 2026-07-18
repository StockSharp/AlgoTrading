# Estratégia SuperTrend de Reversão de Pivot Aprimorada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina a direção do SuperTrend com rompimentos de máximos/mínimos de pivot. Um stop de compra é colocado acima de um máximo de pivot recente quando o SuperTrend está baixista. Um stop de venda é colocado abaixo de um mínimo de pivot quando o SuperTrend está altista. As posições são protegidas com um stop-loss percentual a partir do pivot.

## Detalhes

- **Critérios de entrada**:
  - Comprado: Máximo de pivot formado, SuperTrend baixista → stop de compra acima do pivot.
  - Vendido: Mínimo de pivot formado, SuperTrend altista → stop de venda abaixo do pivot.
- **Direção**: Configurável.
- **Critérios de saída**: Stop-loss percentual ou direção oposta para o modo unilateral.
- **Indicadores**: SuperTrend, máximos/mínimos de pivot.
- **Valores padrão**:
  - `LeftBars` = 6
  - `RightBars` = 3
  - `AtrLength` = 5
  - `Factor` = 2.618
  - `StopLossPercent` = 20
  - `TradeDirection` = Both
  - `CandleType` = 5 minute
