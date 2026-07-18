# Estratégia de Sinal Virtual TradePad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria a lógica do painel multi-indicador da ferramenta VirtualTradePad do MetaTrader. Rastreia doze sinais –
baseados em tendência, momentum e canais – e só opera quando um número configurável de indicadores concordam. O objetivo é imitar a
matriz de sentimento visual do painel original e convertê-la em uma estratégia StockSharp totalmente automatizada.

## Como funciona

- **Dados**: opera um único instrumento no tipo de candle selecionado (padrão: 15 minutos).
- **Indicadores**:
  - Médias móveis simples rápida/lenta para a direção do cruzamento.
  - Cruzamento da linha MACD e sinal.
  - Saídas de sobrecompra/sobrevenda do Estocástico %K (níveis 20/80).
  - Reversões nos limites 30/70 do RSI.
  - Reversões nos níveis -100/+100 do CCI.
  - Reversões nos níveis -80/-20 do Williams %R.
  - Rompimento de volta ao interior do canal das Bandas de Bollinger.
  - Rompimento de volta ao interior do canal do Envelope de média móvil.
  - Alinhamento de mandíbula/dentes/lábios do Alligator de Bill Williams.
  - Inclinação da Média Móvel Adaptativa de Kaufman (ascendente/descendente).
  - Cruzamentos da linha zero do Awesome Oscillator.
  - Cruzamento Tenkan-Kijun do Ichimoku.
- Cada indicador produz um voto de compra (+1), venda (-1) ou neutro (0). Quando o número de votos de compra (ou venda) atinge o
  parâmetro **MinimumConfirmations** e supera o lado oposto, a estratégia abre uma posição nessa direção.
- A opção **CloseOnOpposite** fecha a posição quando a contagem de votos opostos atinge o limiar.
- **Gestão de risco**: take profit e stop loss opcionais definidos em passos de preço do instrumento.

## Parâmetros

- `FastMaLength`, `SlowMaLength` – comprimentos das médias móveis para o cruzamento.
- `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` – configuração do MACD.
- `StochasticLength`, `StochasticDLength`, `StochasticSlowing` – configuração do oscilador Estocástico.
- `RsiLength`, `CciLength`, `WilliamsLength` – lookbacks dos osciladores.
- `BollingerLength`, `BollingerDeviation` – Bandas de Bollinger.
- `EnvelopeLength`, `EnvelopeDeviation` – Envelopes percentuais em torno da SMA.
- `AlligatorJawLength`, `AlligatorTeethLength`, `AlligatorLipsLength` – SMMAs do Alligator.
- `AmaLength`, `AmaFastPeriod`, `AmaSlowPeriod` – configuração do AMA de Kaufman.
- `IchimokuTenkanLength`, `IchimokuKijunLength`, `IchimokuSenkouLength` – linhas do Ichimoku.
- `AoShortPeriod`, `AoLongPeriod` – janelas do Awesome Oscillator.
- `MinimumConfirmations` – número de sinais alinhados necessários para entrar.
- `AllowLong`, `AllowShort` – habilitar lados comprado/vendido.
- `CloseOnOpposite` – sair quando a contagem de votos opostos satisfaz o limiar.
- `TakeProfitPips`, `StopLossPips` – alvos de risco opcionais em passos de preço (0 desativa).
- `CandleType` – período/tipo de dado para análise.

## Resumo da lógica de trading

1. Atualizar todos os indicadores quando um candle fecha.
2. Contar os votos altistas e baixistas dos indicadores.
3. Entrar comprado/vendido quando os votos atingem o limiar de confirmação e superam o lado oposto.
4. Opcionalmente fechar quando o lado oposto atinge o limiar.
5. Aplicar take profit/stop loss opcionais medidos em passos de preço.

A estratégia é projetada para traders discricionários que gostavam do painel de sentimento do VirtualTradePad, mas desejam uma
implementação automatizada dentro do framework StockSharp.
