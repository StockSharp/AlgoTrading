# FX Carry Trade

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Carry mensal = BaseRate − QuoteRate anual observado. Comprar os TopK diferenciais superiores e vender os TopK inferiores; exigir pelo menos 2×TopK pares elegíveis sem sobreposição. Cada lado recebe metade do orçamento bruto em USD, igualmente dividida. Converter exposição USD em unidades nativas com UsdPerUnit observado explicitamente, inclusive quando a moeda de cotação não é USD. Liquidar os pares que saem da cesta e ajustar os demais na próxima revisão.

Somente velas encerradas sincronizadas são avaliadas. As estatísticas contam datas UTC distintas; novas velas intradiárias substituem o valor da mesma data. Há um rebalanceamento na primeira vela conjunta apta de cada mês; dados incompletos adiam a decisão. Empates são ordenados por Security.Id.

Observações devem ser publicadas até o fechamento da vela de sinal. No intradiário contam apenas datas UTC anteriores; uma vela diária completa contribui sua própria data. Datas CSV futuras não antecipam decisões.

O limite de publicação é o último CloseTime real entre as velas completas sincronizadas. Uma sessão pode terminar antes de transcorrer todo o intervalo configurado.

## Dados de entrada

RatesFile é CSV obrigatório: `SecurityId,PublishedAt,BaseRate,QuoteRate,UsdPerUnit`. PublishedAt: UTC `yyyy-MM-ddTHH:mm:ssZ`; taxas nas mesmas unidades decimais anuais. UsdPerUnit é o valor USD positivo de uma unidade negociada. Apenas a última publicação com idade <= MaxRateAgeDays é elegível. Duplicatas ativo/publicação são rejeitadas. Taxas não são fabricadas com velas.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `Universe` | obrigatório |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `TopK` | `3` |
| `RatesFile` | obrigatório |
| `MaxRateAgeDays` | `45` |

## Execução

Capital é orçamento USD fixo e Leverage controla exposição bruta. Usar RatesFile.UsdPerUnit para quantidades e MinTradeUsd. Arredondar para zero em VolumeStep, conciliar execuções reais e aguardar ordens pendentes. Respeitar MinVolume/MaxVolume e dividir ajustes grandes entre velas posteriores. Liquidação completa ignora MinTradeUsd. Sem stop-loss ou realização de lucro.

## Verificação

Ambas as linguagens usam execuções BTC/TON incluídas e fixtures explícitas de taxas, relatórios, pontuações, textos e aquecimento. Fixtures de calendário deslocam OHLC/volume e definem cotações; não são histórico plurianual. Fórmulas independentes, direção/tamanho das ordens, execuções reais, posições residuais e datas proibidas são verificadas. Dois grupos, K=1 e AssetDecile=1 são perfis declarados de mecânica, não prova do universo completo. Sem promessa de rentabilidade.
