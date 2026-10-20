# Accrual Anomaly

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Ordenar relatórios anuais por (NetIncome − OperatingCashFlow) / TotalAssets. Comprar o grupo inferior de Deciles e vender o superior. Cada extremo contém floor(quantidade elegível / Deciles) ativos; exigem-se ao menos Deciles relatórios. O mês anual padrão é maio.

O rebalanceamento ocorre uma vez ao ano, em RebalanceMonth, na primeira vela conjunta encerrada apta. Relatórios publicados depois não alteram decisões anteriores. MaxReportAgeDays conta desde PeriodEnd.

## Dados de entrada

FundamentalsFile é obrigatório: SecurityId,PeriodEnd,PublishedAt,TotalAssets,NetIncome,OperatingCashFlow,BookEquity,SharesOutstanding. PeriodEnd: yyyy-MM-dd; PublishedAt: yyyy-MM-ddTHH:mm:ssZ (UTC). Ativos e ações devem ser positivos. Seleciona-se o período mais recente e a última revisão publicada até o sinal. Revisões duplicadas e publicações antes do fim do período são rejeitadas. Não se extraem fundamentos de velas.

## Parâmetros

| Parâmetro | Valor padrão |
|---|---|
| `Universe` | obrigatório |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `50` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `Deciles` | `10` |
| `FundamentalsFile` | obrigatório |
| `RebalanceMonth` | `5` |
| `MaxReportAgeDays` | `550` |

## Execução

Universe é definido explicitamente. Preços e Capital usam USD por unidade negociada. Capital é um orçamento fixo; Leverage define a exposição bruta. Não são adicionados stop loss nem take profit.

Cada cesta recebe metade de Capital × Leverage, dividida igualmente. As unidades são calculadas pelo fechamento do sinal e arredondadas para zero conforme VolumeStep. Usam-se posições realmente executadas, aguardam-se ordens pendentes e respeitam-se MinVolume/MaxVolume, dividindo grandes ajustes entre velas posteriores. MinTradeUsd filtra pequenos ajustes, mas não liquidações completas.

## Verificação

As execuções usam apenas o arquivo BTC/TON existente. Relatórios, observações de opções e histórico de aquecimento explícitos verificam a mecânica e não são dados reais de empresas ou opções. Janelas menores, dois grupos e março como mês anual são perfis de teste identificados, não prova dos padrões originais de longo prazo. Não se afirma rentabilidade.
