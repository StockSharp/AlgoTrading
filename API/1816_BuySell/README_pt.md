# Estratégia BuySell
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia emula o especialista **BuySell** do MetaTrader. Combina uma média móvel com o Average True Range (ATR) para detectar reversões de tendência.
Quando a média móvel vira para cima, o sistema considera o mercado altista; quando vira para baixo, considera o mercado baixista.
Uma operação é aberta apenas se a vela anterior estava no estado oposto, confirmando uma reversão. Os níveis opcionais de stop-loss e take-profit são expressos em pontos de preço.

## Detalhes

- **Lógica de entrada**
  - **Comprado**: a média móvel passa de descendente para ascendente e a vela anterior era baixista.
  - **Vendido**: a média móvel passa de ascendente para descendente e a vela anterior era altista.
- **Lógica de saída**
  - **Comprado**: a média móvel vira para baixo ou o stop-loss / take-profit é acionado.
  - **Vendido**: a média móvel vira para cima ou o stop-loss / take-profit é acionado.
- **Indicadores**: Média Móvel Simples (SMA) e ATR.
- **Stops**: Stop-loss e take-profit em pontos.
- **Permissões**: flags separadas permitem ou proíbem abrir/fechar posições compradas e vendidas.
- **Período padrão**: velas de 4 horas.

## Parâmetros

| Nome | Padrão | Descrição |
| ---- | ------ | --------- |
| `MaPeriod` | 14 | Período da média móvel. |
| `AtrPeriod` | 60 | Período do ATR. |
| `StopLoss` | 1000 | Stop-loss em pontos de preço. |
| `TakeProfit` | 2000 | Take-profit em pontos de preço. |
| `AllowLongEntry` | true | Permissão para abrir posições compradas. |
| `AllowShortEntry` | true | Permissão para abrir posições vendidas. |
| `AllowLongExit` | true | Permissão para fechar posições compradas. |
| `AllowShortExit` | true | Permissão para fechar posições vendidas. |
| `CandleType` | H4 | Período utilizado para os cálculos. |

## Uso

1. Adicione a estratégia à sua solução StockSharp.
2. Configure os parâmetros conforme necessário.
3. Execute a estratégia no modo ao vivo ou de backtesting. As operações são executadas usando ordens `BuyMarket` e `SellMarket`.

A abordagem é adequada para mercados onde as reversões de tendência são acompanhadas por mudanças de volatilidade capturadas pelo ATR.
