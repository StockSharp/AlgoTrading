# Lexical Density Filings

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Ler o último relatório de texto publicado por ativo. Tokens são sequências Unicode de letras/marcas, normalizadas pelas regras invariantes .NET. Densidade lexical = tokens normalizados distintos / tokens totais. Revisar somente nas primeiras três datas de negociação de fevereiro, maio, agosto e novembro pelo calendário do primeiro ativo. Aguardar nessa janela até existirem Quintile relatórios; caso contrário, aguardar o próximo mês de revisão. Comprar o grupo de maior densidade e vender o menor, floor(quantidade elegível / Quintile) por lado. Manter entre revisões.

Observações devem ser publicadas até o fechamento da vela de sinal. No intradiário contam apenas datas UTC anteriores; uma vela diária completa contribui sua própria data. Datas CSV futuras não antecipam decisões.

O limite de publicação é o último CloseTime real entre as velas completas sincronizadas. Uma sessão pode terminar antes de transcorrer todo o intervalo configurado.

## Dados de entrada

FilingsFile é obrigatório: `SecurityId,PeriodEnd,PublishedAt,DocumentPath`. Formatos: `yyyy-MM-dd` e UTC `yyyy-MM-ddTHH:mm:ssZ`. DocumentPath indica texto UTF-8, absoluto ou relativo ao manifesto; sem vírgulas nos caminhos. Rejeitar textos vazios, revisões duplicadas e publicação antes de PeriodEnd. Usar o último período/revisão publicado até o fechamento e limite MaxReportAgeDays. O primeiro ativo precisa de calendário com feriados.

Os tokens usam elementos de texto Unicode cuja categoria inicial é letra ou marca combinante; pontuação e dígitos separam palavras. Normalização de caixa invariante, sem radicalização ou normalização de acentos.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `Universe` | obrigatório |
| `Capital` | `100000` |
| `Leverage` | `1` |
| `MinTradeUsd` | `200` |
| `CandleType` | `TimeSpan.FromDays(1).TimeFrame()` |
| `Quintile` | `5` |
| `MaxReportAgeDays` | `550` |
| `FilingsFile` | obrigatório |

## Execução

Universe é definido explicitamente. Preços e Capital usam USD por unidade negociada. Capital é um orçamento fixo; Leverage define a exposição bruta. Não são adicionados stop loss nem take profit.

Cada cesta recebe metade de Capital × Leverage, dividida igualmente. As unidades são calculadas pelo fechamento do sinal e arredondadas para zero conforme VolumeStep. Usam-se posições realmente executadas, aguardam-se ordens pendentes e respeitam-se MinVolume/MaxVolume, dividindo grandes ajustes entre velas posteriores. MinTradeUsd filtra pequenos ajustes, mas não liquidações completas.

## Verificação

Ambas as linguagens usam execuções BTC/TON incluídas e fixtures explícitas de taxas, relatórios, pontuações, textos e aquecimento. Fixtures de calendário deslocam OHLC/volume e definem cotações; não são histórico plurianual. Fórmulas independentes, direção/tamanho das ordens, execuções reais, posições residuais e datas proibidas são verificadas. Dois grupos, K=1 e AssetDecile=1 são perfis declarados de mecânica, não prova do universo completo. Sem promessa de rentabilidade.
