# Ema50 Crossover Monthly Dca

[English](README.md) | [Русский](README_ru.md) | [Deutsch](README_de.md) | [Español](README_es.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Regras de negociação

Velas semanais encerradas e EMA formada de EmaLength fechamentos, iniciada pela média do primeiro período completo. Fechamentos elegíveis desde StartDate. Creditar um DcaAmount no primeiro mês elegível observado e depois pelos meses decorridos, inclusive no aquecimento. Não inventar décadas de aportes anteriores ao primeiro dado. Acima da EMA investir caixa acumulado com quantidades arredondadas; abaixo vender toda compra real; igualdade mantém. Saldo acompanha execuções brutas e vendas; é registro de alocação, não depósito ou financiamento. Aguardar ordens, respeitar limites e manter caixa restante. Sem vendas a descoberto, RSI, espera ou stops.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(10080).TimeFrame() |
| `EmaLength` | 50 |
| `DcaAmount` | 100000 |
| `StartDate` | 1980-01-01 |

## Verificação

Ambas as linguagens são verificadas com BTC/TON incluído, fórmulas independentes, direção/tamanho das ordens, execuções, posições e saídas. Dados datados de preços/fatores e calendário verificam mecânica; OHLC/volumes deslocados não são história plurianual. Dois grupos, TopN=1 e SMA=2 são perfis pequenos explícitos. Janelas longas e valores originais são verificados separadamente. Sem promessa de rentabilidade.
