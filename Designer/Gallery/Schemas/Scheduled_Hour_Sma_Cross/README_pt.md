# Cruzamento de SMA em hora programada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama avalia uma média móvel rápida e outra lenta em candles concluídos de trinta minutos do BTCUSDT, permite entradas programadas quando a hora de abertura do candle é 12, fecha posições contrárias fora dessa hora e separa as ações a mercado com uma espera de oito candles.

![schema](schema.svg)

## Visão geral da estratégia

- Candles concluídos de trinta minutos alimentam SMA(8) e SMA(21), que emitem apenas valores formados. Nenhuma decisão é liberada até que os dois valores existam para o mesmo candle.
- Uma tendência de alta significa `SMA(8) > SMA(21)`; uma tendência de baixa significa `SMA(8) < SMA(21)`. Valores iguais não geram ação.
- O bloco Time fornece o timestamp da decisão e Converter extrai seu Hour. Em cada lote de candle concluído, esse timestamp se alinha ao `OpenTime` usado pela regra. Um Flag de uso único permite exatamente uma decisão para o candle, inclusive durante eventos síncronos de ordens.
- Um estado acionado por execuções registra a posição líquida como `-1`, `0` ou `1`. Cada um dos quatro caminhos prepara seu próprio próximo estado antes de enviar uma ordem e só o confirma quando esse caminho informa uma execução.
- Cada ação inicia uma espera de oito candles. Os candles seguintes 1 a 7 permanecem bloqueados; o oitavo candle concluído posterior reduz o contador a zero antes de decidir e volta a ser elegível.

## Regras de entrada e saída

- **Ação programada de alta**: Em um candle cujo `OpenTime.Hour` é 12, uma tendência de alta com posição zerada ou vendida envia uma compra a mercado de Volume 1. Uma posição vendida é reduzida a zero, sem reversão direta.
- **Ação programada de baixa**: Na mesma hora, uma tendência de baixa com posição zerada ou comprada envia uma venda a mercado de Volume 1. Uma posição comprada é reduzida a zero, sem reversão direta.
- **Saída fora da hora**: Em qualquer outra hora de abertura, uma tendência de baixa fecha uma posição comprada com uma venda a mercado, enquanto uma tendência de alta fecha uma posição vendida com uma compra a mercado. Nenhuma nova posição é aberta fora da hora 12.
- Não há uma hora de fechamento separada. Os quatro ramos exigem que a espera esteja disponível, e somente um ramo verdadeiro pode acionar seu bloco Modify position independente.

## Parâmetros

| Parâmetro | Valor padrão | Descrição |
|---|---|---|
| BTC Data Security | BTCUSDT@BNBFT | Instrumento usado por Candles e Strategy trades. Defina Strategy Security como o mesmo instrumento para as transações. |
| Candle Series | 00:30:00 | Intervalo dos candles concluídos e relógio para atualizações dos indicadores e passos da espera. |
| Fast SMA Length | 8 | Quantidade de candles concluídos na média móvel simples rápida. |
| Slow SMA Length | 21 | Quantidade de candles concluídos na média móvel simples lenta. |
| Trade Hour | 12 | Valor aceito do campo `OpenTime.Hour` do candle concluído para ações de entrada programadas. |
| Cooldown N | 8 | Primeiro índice de candle concluído posterior em que outra ação pode ser considerada. |
| Volume | 1 | Quantidade fixa de cada compra ou venda a mercado. |

## Detalhes do diagrama

- A [Variable](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) do BTC envia o instrumento para [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) construídos e concluídos e para [Strategy trades](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html).
- Dois blocos [Indicator](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) com valores formados calculam SMA(8) e SMA(21). Blocos Formula expõem os valores numéricos aos blocos [Comparison](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) de alta e de baixa.
- Time libera primeiro a posição armazenada, a hora programada, a referência zero, o estado da espera, seu timestamp para o Converter de Hour e o volume fixo. Por último, aciona o registro de decisão pendente, para que cada porta lógica de cinco entradas receba um retrato coerente do candle.
- O Flag de uso único impede reentrada durante o processamento síncrono das transações. Quatro blocos [Modify position](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) separados evitam que um ramo falso consuma o volume preparado para um ramo verdadeiro.
- Os estados candidatos da compra e da venda programadas são `posição + 1` e `posição - 1`; os dois candidatos de saída são zero. Um candidato só chega ao estado da posição pela saída `MyTrade` do Modify position correspondente.
- O bloco [Delay](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) recebe cada candle antes das decisões desse mesmo candle. Uma porta de ação verdadeira primeiro marca a espera como indisponível e arma N = 8, depois envia a ordem. O gráfico mostra candles, as duas médias, posição executada, quatro fluxos de execuções das ações e todas as execuções da estratégia.

## Uso

Importe o arquivo `.json` no Designer, defina Strategy Security como BTCUSDT@BNBFT, escolha um portfólio e execute o diagrama com histórico de trinta minutos. Com os dados de março incluídos e os valores indicados, a validação produziu 59 ordens a mercado concluídas e 59 execuções sem erros de transação. Confira o fuso horário dos candles, o campo da hora, o volume e o comportamento da espera antes de operar ao vivo.
