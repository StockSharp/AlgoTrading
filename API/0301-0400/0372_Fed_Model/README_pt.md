# Fed Model

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Ordem de Universe: [ETF de ações, ETF monetário opcional]. Usar velas diárias e o calendário do primeiro ativo. No fechamento do último dia de negociação do mês, exigir RegressionMonths observações mensais consecutivas publicadas até esse fechamento. Regressão com intercepto: y = EarningsYield − BondYield, x = 0..N−1; slope = Σ((x−mean(x))×(y−mean(y))) / Σ(x−mean(x))²; previsão = mean(y) + slope×(N−mean(x)). Positivo mantém ações; negativo mantém ETF monetário ou caixa. Zero preserva o regime anterior. Operar somente na mudança de regime, sem redimensionamento mensal. Concluir a liquidação anterior antes da compra.

Observações devem ser publicadas até o fechamento da vela de sinal. No intradiário contam apenas datas UTC anteriores; uma vela diária completa contribui sua própria data. Datas CSV futuras não antecipam decisões.

O limite de publicação é o último CloseTime real entre as velas completas sincronizadas. Uma sessão pode terminar antes de transcorrer todo o intervalo configurado.

## Dados de entrada

YieldHistoryFile é obrigatório: `Month,PublishedAt,EarningsYield,BondYield`. Month: `yyyy-MM`; PublishedAt: UTC `yyyy-MM-ddTHH:mm:ssZ`. Rendimentos nas mesmas unidades. Apenas revisões publicadas até o fechamento da decisão. Exigir meses consecutivos até o mês atual; ausência adia a previsão. Rejeitar revisões duplicadas e publicação anterior ao mês observado. Usar dados observados de mercado/fundamentais, não preços inversos.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `Universe` | obrigatório |
| `Capital` | `100000` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `RegressionMonths` | `12` |
| `YieldHistoryFile` | obrigatório |

## Execução

Definir Universe explicitamente. Preços e Capital em USD por unidade negociada. Exposição comprada limitada a min(Capital, Portfolio.CurrentValue informado); sem valor positivo não há orçamento de compra. Sem alavancagem, stop-loss ou realização de lucro.

Arredondar objetivos para zero em VolumeStep. Aguardar execuções da liquidação completa antes de abrir novas posições; conciliar posições reais, respeitar MinVolume/MaxVolume e dividir ajustes grandes entre velas posteriores. MinTradeUsd filtra ajustes de objetivos não nulos; liquidação completa ignora o filtro.

## Verificação

Ambas as linguagens usam execuções BTC/TON incluídas e fixtures explícitas de taxas, relatórios, pontuações, textos e aquecimento. Fixtures de calendário deslocam OHLC/volume e definem cotações; não são histórico plurianual. Fórmulas independentes, direção/tamanho das ordens, execuções reais, posições residuais e datas proibidas são verificadas. Dois grupos, K=1 e AssetDecile=1 são perfis declarados de mecânica, não prova do universo completo. Sem promessa de rentabilidade.
