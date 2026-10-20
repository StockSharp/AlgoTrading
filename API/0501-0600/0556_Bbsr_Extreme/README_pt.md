# BBSR Extreme

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Retorno para dentro das bandas com filtro de inclinação EMA e proteção ATR. `C,L,U,E` indicam fechamento original, banda inferior, superior e EMA; `p` indica a vela formada anterior.

## Regras de negociação

Compra: `C[p] < L[p] && C > L && E > E[p]`. Venda: `C[p] > U[p] && C < U && E < E[p]`. A igualdade não gera entrada. Entrada apenas sem posição, por `Volume`, sem acumulação, reversão ou pausa fixa.

Antes da entrada, fixa-se `A=ATR` da vela de sinal. A proteção local nativa usa o preço efetivamente executado: compra, stop/alvo `fill-2A` / `fill+3A`; venda, `fill+2A` / `fill-3A`. Os multiplicadores são parâmetros; ATR posterior não muda os níveis. Assinaturas bid/ask permitem encerrar a quantidade executada entre velas com ordens a mercado.

As entradas exigem velas concluídas e indicadores aquecidos. A posição é acompanhada até a saída; ordens a mercado pendentes impedem envios duplicados.

Bollinger usa o desvio padrão populacional da janela móvel de fechamentos, incluindo a vela concluída atual.

## Parâmetros

| Parameter | Default |
|---|---|
| `BollingerPeriod` | `20` |
| `BollingerMultiplier` | `2` |
| `MaLength` | `7 (EMA)` |
| `AtrLength` | `14 (Wilder)` |
| `AtrStopMultiplier` | `2` |
| `AtrProfitMultiplier` | `3` |
| `CandleType` | `5m` |
