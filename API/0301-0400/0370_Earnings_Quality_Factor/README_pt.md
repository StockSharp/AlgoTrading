# Earnings Quality Factor

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Qualidade dos lucros = (OperatingCashFlow − NetIncome) / TotalAssets. Na primeira vela sincronizada elegível de RebalanceMonth, usar o período fiscal mais recente e sua última revisão publicada até o fechamento. Comprar o grupo Deciles de maior qualidade e vender o de menor qualidade: floor(quantidade elegível / Deciles) ativos por lado. Rebalancear uma vez por ano e manter as posições até a próxima revisão. A idade desde PeriodEnd não pode exceder MaxReportAgeDays.

O rebalanceamento ocorre uma vez ao ano, em RebalanceMonth, na primeira vela conjunta encerrada apta. Relatórios publicados depois não alteram decisões anteriores. MaxReportAgeDays conta desde PeriodEnd.

Observações devem ser publicadas até o fechamento da vela de sinal. No intradiário contam apenas datas UTC anteriores; uma vela diária completa contribui sua própria data. Datas CSV futuras não antecipam decisões.

O limite de publicação é o último CloseTime real entre as velas completas sincronizadas. Uma sessão pode terminar antes de transcorrer todo o intervalo configurado.

## Dados de entrada

FundamentalsFile é obrigatório: SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding. PeriodEnd: yyyy-MM-dd; PublishedAt: yyyy-MM-ddTHH:mm:ssZ (UTC). Ativos e ações devem ser positivos. Seleciona-se o período mais recente e a última revisão publicada até o sinal. Revisões duplicadas e publicações antes do fim do período são rejeitadas. Não se extraem fundamentos de velas.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `Universe` | obrigatório |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `100` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `RebalanceMonth` | `7` |
| `MaxReportAgeDays` | `550` |
| `FundamentalsFile` | obrigatório |

## Execução

Universe é definido explicitamente. Preços e Capital usam USD por unidade negociada. Capital é um orçamento fixo; Leverage define a exposição bruta. Não são adicionados stop loss nem take profit.

Cada cesta recebe metade de Capital × Leverage, dividida igualmente. As unidades são calculadas pelo fechamento do sinal e arredondadas para zero conforme VolumeStep. Usam-se posições realmente executadas, aguardam-se ordens pendentes e respeitam-se MinVolume/MaxVolume, dividindo grandes ajustes entre velas posteriores. MinTradeUsd filtra pequenos ajustes, mas não liquidações completas.

## Verificação

Ambas as linguagens usam execuções BTC/TON incluídas e fixtures explícitas de taxas, relatórios, pontuações, textos e aquecimento. Fixtures de calendário deslocam OHLC/volume e definem cotações; não são histórico plurianual. Fórmulas independentes, direção/tamanho das ordens, execuções reais, posições residuais e datas proibidas são verificadas. Dois grupos, K=1 e AssetDecile=1 são perfis declarados de mecânica, não prova do universo completo. Sem promessa de rentabilidade.
