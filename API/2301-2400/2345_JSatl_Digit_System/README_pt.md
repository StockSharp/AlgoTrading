# Estratégia JSatl Sistema Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este exemplo demonstra uma adaptação simplificada do consultor especialista MQL5 "JSatl Digit System" para o StockSharp.

A estratégia usa a Média Móvel Jurik (JMA) para criar um estado de tendência digital:

- Quando o preço de fechamento está acima da JMA, o estado se torna **ascendente**.
- Quando o preço de fechamento está abaixo da JMA, o estado se torna **descendente**.

Se o estado muda para ascendente, posições vendidas podem ser fechadas e/ou uma posição comprada pode ser aberta dependendo dos parâmetros. Quando o estado muda para descendente, posições compradas podem ser fechadas e/ou uma posição vendida pode ser aberta.

**Parâmetros**

- `JmaLength` – período da JMA.
- `CandleType` – série de velas usada para os cálculos.
- `StopLossPercent` – stop-loss protetor em percentual.
- `TakeProfitPercent` – take-profit protetor em percentual.
- `BuyPosOpen`, `SellPosOpen`, `BuyPosClose`, `SellPosClose` – habilitar ou desabilitar ações para os sinais correspondentes.
