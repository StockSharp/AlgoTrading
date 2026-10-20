# BB RSI Trailing Stop

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma excursão de Bollinger exige recuperação do RSI, seguida por stop móvel condicional. `p` é a vela formada anterior; RSI usa suavização Wilder.

## Regras de negociação

Compra: `Low < lowerBand` e `RSI[p] <= RsiOversold && RSI > RsiOversold`. Venda: `High > upperBand` e `RSI[p] >= RsiOverbought && RSI < RsiOverbought`. Continuar em sobrevenda/sobrecompra não basta. Entrada por `Volume` apenas sem posição, sem espera oculta.

A proteção parte do preço médio ponderado realmente executado: bid para compra, ask para venda. Distância inicial `StopLossPoints`; o acompanhamento só começa após extremo favorável de `TrailOffsetPoints`. Depois, o stop segue esse extremo a `TrailStopPoints`, sem afrouxar o stop inicial. Ao cruzar a cotação, toda a posição executada é encerrada a mercado. As distâncias são unidades absolutas de preço, não ticks ou percentagens; extremos anteriores da vela não acionam stop criado posteriormente.

As entradas exigem velas concluídas e indicadores aquecidos. A posição é acompanhada até a saída; ordens a mercado pendentes impedem envios duplicados.

Bollinger usa o desvio padrão populacional da janela móvel de fechamentos, incluindo a vela concluída atual.

## Parâmetros

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `25` |
| `BollingerDeviation` | `2` |
| `RsiPeriod` | `14` |
| `RsiOversold` | `33` |
| `RsiOverbought` | `60` |
| `StopLossPoints` | `50` |
| `TrailOffsetPoints` | `99` |
| `TrailStopPoints` | `40` |
| `CandleType` | `15m` |
