# Diagrama da estratégia de reversão à média com Internal Bar Strength (IBS)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Internal Bar Strength faz uma única pergunta sobre um candle finalizado: em que ponto da própria amplitude ele fechou? Zero significa fechamento na mínima, um significa fechamento na máxima. Este diagrama só vende, e só contra a força: um candle que rompe a máxima anterior e ainda assim termina colado ao topo da sua amplitude é lido como um movimento esticado prestes a devolver parte do caminho.

![schema](schema.svg)

## Visão geral da estratégia

- Aqui o IBS não é um bloco de indicador, e sim uma fórmula: (Fechamento - Mínima) dividido pela amplitude do mesmo candle, de modo que toda a medida cabe em uma expressão legível.
- Um bloco de valor anterior guarda a máxima do candle anterior, que é a referência da condição de rompimento.
- A estratégia é vendida por concepção: o bloco de compra existe apenas para encerrar a venda e nunca abre uma compra.
- Não há stop nem alvo — a operação fica inteiramente a cargo do segundo limiar de IBS.

## Regras de entrada e saída

- **Entrada comprada**: Não há entrada comprada. O diagrama apenas vende, exatamente como a estratégia original.
- **Entrada vendida**: O candle fechou acima da máxima do candle anterior, seu IBS está no limiar superior ou acima dele e a posição ainda não está vendida. A ordem vende um lote e abre uma venda.
- **Saída**: A venda é recomprada quando o IBS de um candle cai ao limiar inferior ou abaixo dele, isto é, quando o fechamento volta à parte baixa da própria amplitude; a compra roda em modo de fechamento, então zera a posição em vez de invertê-la. O original não tem stop loss nem take profit, e nenhum foi acrescentado aqui. Dois detalhes divergem do código. O original trabalha com candles de quatro horas, dos quais o histórico incluído de um mês daria apenas algumas centenas, por isso o diagrama passa a candles de cinco minutos. E o original simplesmente pula o candle cuja máxima é igual à mínima; aqui a fórmula divide por uma amplitude limitada por baixo a um passo de preço, o que dá IBS zero nesse candle e o mantém fora das duas condições. A SimpleMovingAverage que o original cria não é reproduzida, porque o valor dela não entra em nenhuma decisão lá.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Upper IBS Threshold | 0.9 | Nível de IBS em que, ou acima do qual, o candle de rompimento é vendido. |
| Lower IBS Threshold | 0.3 | Nível de IBS em que, ou abaixo do qual, a venda é recomprada. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha; o original usa candles de quatro horas e este diagrama os de cinco minutos do histórico incluído. |

## Detalhes do diagrama

- Três conversores extraem do bloco de candles o fechamento, a máxima e a mínima de cada candle finalizado.
- Um bloco de fórmula transforma esses três números em Internal Bar Strength, com a amplitude limitada por baixo para que um candle plano não divida por zero.
- Um bloco de valor anterior atrasa a máxima em um candle e uma comparação mede o fechamento contra ela — essa é a metade de rompimento da entrada.
- O bloco de posição é comparado duas vezes com uma constante zero: uma proteção deixa a entrada passar apenas enquanto não há venda aberta, a outra permite a saída somente quando a venda existe.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
