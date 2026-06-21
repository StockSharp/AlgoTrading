# Ordens Pendentes Automáticas por RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia coloca ordens limite pendentes após o Índice de Força Relativa (RSI) permanecer em zonas extremas por várias velas consecutivas.

Quando o RSI permanece abaixo do nível de sobrevenda por `MatchCount` velas, uma ordem de compra limite é registrada abaixo do fechamento da vela em `PendingOffset` pontos de preço. Quando o RSI permanece acima do nível de sobrecompra pelo mesmo número de velas, uma ordem de venda limite é colocada acima do fechamento com o mesmo deslocamento.

## Parâmetros
- `RsiPeriod` – período de cálculo do RSI.
- `RsiOverbought` – nível que define a zona de sobrecompra.
- `RsiOversold` – nível que define a zona de sobrevenda.
- `PendingOffset` – distância do preço de fechamento para colocar ordens pendentes (pontos de preço).
- `MatchCount` – número de velas consecutivas necessárias antes de colocar as ordens.
- `CandleType` – período de velas utilizado para análise.

Os valores padrão emulam o script MQL original e usam velas de 4 horas.
