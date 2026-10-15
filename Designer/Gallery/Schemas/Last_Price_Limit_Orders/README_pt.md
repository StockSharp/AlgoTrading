# Reversão Last Price com limites Level1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O exemplo trata principalmente de Order registering e execução Level1: expressa a conhecida reversão por desvio da EMA com limites negociáveis. Apesar do nome histórico, toda decisão vem de candle concluído de quatro horas; não existe sinal por ticks.

![schema](schema.svg)

## Visão geral da estratégia

- Fechamentos concluídos de quatro horas alimentam EMA(20) e são comparados com limites 0,5% abaixo e acima.
- Sem posição, fechar abaixo do limite inferior solicita compra e acima do superior solicita venda.
- Long sai quando o fechamento volta à EMA ou acima; short sai ao voltar à EMA ou abaixo.
- Cada candle captura best ask para compras e best bid para vendas; gates de preço positivo evitam registro sem cotação.
- O mesmo volume um alimenta entrada e saída, portanto posição aberta pelo diagrama zera com uma execução oposta.

## Regras de entrada e saída

- **Entrada comprada**: Com Position == 0 e Close < EMA × (1 − 0,5/100), registra compra em best ask. Cruzar o spread busca execução como BuyMarket.
- **Entrada vendida**: Com Position == 0 e Close > EMA × (1 + 0,5/100), registra venda em best bid. Cruzar o spread busca execução como SellMarket.
- **Saída**: Com Position > 0 e Close >= EMA, sell vende uma unidade em best bid; com Position < 0 e Close <= EMA, buy compra uma unidade em best ask.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candle Time Frame | 04:00:00 | Intervalo concluído para EMA e decisões, igual ao padrão C#. |
| EMA Period | 20 | Quantidade de fechamentos em ExponentialMovingAverage. |
| Entry Distance, % | 0.5 | Desvio percentual da EMA exigido para entrar zerado. |
| Shared Entry/Exit Volume | 1 | Volume comum dos limites de entrada e saída. |

## Detalhes do diagrama

- C# usa ordens a mercado. O diagrama preserva Order registering e usa limites negociáveis: compra no best ask e venda no best bid.
- Uma versão passiva compraria no best bid e venderia no best ask, exigindo Order cancellation ou replacement para ordens pendentes.
- Close e EMA pertencem ao mesmo candle. Uma variável libera o fechamento após atualizar a EMA e evita comparação com a média anterior.
- O volume compartilhado espelha Strategy.Volume. Posição externa ou de tamanho diferente não é necessariamente zerada por saída fixa de uma unidade.
- A ideia sobrepõe MA_Deviation; a lição própria é amostragem Level1 e execução por limites negociáveis.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
