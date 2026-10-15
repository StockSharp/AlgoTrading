# Lacust Stop e BE
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia demonstra o gerenciamento básico de posição inspirado no expert advisor MQL original **lacuststopandbe**.

Após entrar em uma posição na direção do último candle concluído, a estratégia aplica várias regras de proteção:

- Stop loss e take profit iniciais são colocados a distâncias de preço fixas.
- Quando o lucro atinge `BreakevenGain`, o stop é movido para o preço de entrada mais `Breakeven`.
- Após o lucro exceder `TrailingStart`, o stop segue o preço à distância de `TrailingStop`.
- A posição é fechada quando o nível de stop ou take profit é tocado.

Parâmetros:

- `CandleType` – série de candles usada para processamento.
- `StopLoss` – distância inicial do stop loss.
- `TakeProfit` – distância inicial do take profit.
- `TrailingStart` – lucro necessário para ativar o trailing stop.
- `TrailingStop` – distância do trailing stop em relação ao preço atual.
- `BreakevenGain` – lucro necessário antes de mover o stop para break-even.
- `Breakeven` – lucro travado após mover o stop para break-even.

Este exemplo usa a API de alto nível do StockSharp e pode servir como modelo para portar scripts simples de gerenciamento de operações MQL.
