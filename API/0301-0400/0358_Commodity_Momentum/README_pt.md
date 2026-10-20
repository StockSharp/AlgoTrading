# Commodity Momentum

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

M é o mês da decisão e P[K] o último fechamento observado do mês completo K. Momentum = P[M − SkipMonths − 1] / P[M − LookbackMonths − 1] − 1. Todos os meses intermediários são exigidos. Em março, os padrões comparam janeiro com fevereiro do ano anterior, excluindo fevereiro e março em curso. Comprar até TopN melhores ativos com pesos iguais, apenas comprados. Momentum negativo é permitido. Fechar removidos no próximo rebalanceamento.

Somente velas encerradas sincronizadas são avaliadas. As estatísticas contam datas UTC distintas; novas velas intradiárias substituem o valor da mesma data. Há um rebalanceamento na primeira vela conjunta apta de cada mês; dados incompletos adiam a decisão. Empates são ordenados por Security.Id.

## Dados de entrada

PriceHistoryFile é um CSV opcional de fechamentos diários positivos: SecurityId,Date,Close; data yyyy-MM-dd. Datas atuais e futuras não antecipam o aquecimento. Fechamentos ao vivo substituem o valor da sua data UTC. Duplicatas são rejeitadas. Permite preparar janelas longas com um arquivo de execução curto.

## Parâmetros

| Parâmetro | Valor padrão |
|---|---|
| `Universe` | obrigatório |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` |
| `PriceHistoryFile` | opcional — não definido |
| `TopN` | `5` |
| `LookbackMonths` | `12` |
| `SkipMonths` | `1` |

## Execução

Universe é definido explicitamente. Preços e Capital usam USD por unidade negociada. Capital é um orçamento fixo; Leverage define a exposição bruta. Não são adicionados stop loss nem take profit.

As unidades são calculadas pelo fechamento do sinal e arredondadas para zero conforme VolumeStep. Usam-se posições realmente executadas, aguardam-se ordens pendentes e respeitam-se MinVolume/MaxVolume, dividindo grandes ajustes entre velas posteriores. MinTradeUsd filtra pequenos ajustes, mas não liquidações completas.

## Verificação

As execuções usam apenas o arquivo BTC/TON existente. Relatórios, observações de opções e histórico de aquecimento explícitos verificam a mecânica e não são dados reais de empresas ou opções. Janelas menores, dois grupos e março como mês anual são perfis de teste identificados, não prova dos padrões originais de longo prazo. Não se afirma rentabilidade.
