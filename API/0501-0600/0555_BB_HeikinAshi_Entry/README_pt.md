# BB HeikinAshi Entry

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Reversão de Bollinger em velas Heikin Ashi, realizando metade da posição e acompanhando o restante.

## Regras de negociação

Fechamento HA: `(Open+High+Low+Close)/4`. Primeira abertura HA: `(Open+Close)/2`; seguintes: `(abertura HA anterior+fechamento HA anterior)/2`. Extremos HA incluem abertura/fechamento HA e extremos originais. Bollinger usa fechamentos HA desde a primeira vela arquivada, não os originais. Doji não tem cor. Toque: `HA low <= lower` / `HA high >= upper`.

Exatamente duas ou três velas HA baixistas consecutivas devem tocar cada uma sua banda inferior. A próxima HA altista deve fechar estritamente acima de sua banda inferior. Uma vela ou quatro ou mais não qualificam. A condição simétrica abre venda.

Entrada por `Volume` apenas sem posição. Stop inicial no mínimo/máximo da vela original concluída anterior, fixado antes da ordem. `R` é a distância positiva entre o preço médio ponderado executado e esse stop. Em `1R`, encerra-se metade da posição executada a mercado, arredondada para baixo por `VolumeStep`. Se qualquer metade for menor que `MinVolume`, encerra-se tudo no alvo. Entrada executada além do stop é encerrada na próxima cotação executável.

Somente execução real do alvo ativa o acompanhamento pelo mínimo/máximo da última vela original concluída, sem afrouxar o stop. Bid para compras ou ask para vendas ativam a saída total entre velas; stop tem prioridade sobre alvo. Ordens pendentes impedem duplicatas. Extremos anteriores não acionam níveis criados depois. Sem alvo percentual fixo nem espera oculta.

As entradas exigem velas concluídas e indicadores aquecidos. A posição é acompanhada até a saída; ordens a mercado pendentes impedem envios duplicados.

Bollinger usa o desvio padrão populacional da janela móvel de fechamentos, incluindo a vela concluída atual.

## Parâmetros

| Parameter | Default |
|---|---|
| `BollingerLength` | `20` |
| `BollingerWidth` | `2` |
| `CandleType` | `15m` |
