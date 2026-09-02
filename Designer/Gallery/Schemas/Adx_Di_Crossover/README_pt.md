# Diagrama da estratégia de cruzamento de ADX e linhas DI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O sistema de movimento direcional de Welles Wilder reunido em um único diagrama. O bloco Average Directional Index entrega três números de uma vez: a linha +DI, a linha -DI e a própria linha ADX. O cruzamento das linhas direcionais escolhe o lado da operação, enquanto a linha ADX decide se o mercado tem tendência suficiente para entrar.

![schema](schema.svg)

## Visão geral da estratégia

- Um único bloco AverageDirectionalIndex alimenta três conversores que extraem +DI, -DI e a linha ADX do mesmo valor complexo do indicador.
- O bloco de cruzamento observa +DI contra -DI e dispara apenas no candle em que as duas linhas realmente trocam de lugar.
- A linha ADX precisa estar no limiar ou acima dele, de modo que trechos laterais e sem direção são filtrados.
- Um bloco de fórmula soma o módulo da posição ao volume base, assim uma única ordem a mercado fecha o lado antigo e abre o novo.

## Regras de entrada e saída

- **Entrada comprada**: +DI cruza acima de -DI, a linha ADX está no limiar ou acima e a posição ainda não está comprada. A ordem compra o volume base mais o tamanho da venda: inverte uma venda ou abre uma compra a partir do zero.
- **Entrada vendida**: +DI cruza abaixo de -DI, a linha ADX está no limiar ou acima e a posição ainda não está vendida. A ordem vende o volume base mais o tamanho da compra: inverte uma compra ou abre uma venda a partir do zero.
- **Saída**: Não há bloco de saída próprio. A posição permanece até o cruzamento contrário das linhas DI, e a ordem de inversão a fecha e abre a oposta ao mesmo tempo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| ADX Period | 14 | Período de suavização compartilhado pela linha ADX e pelo par +DI/-DI. |
| ADX Threshold | 15 | Menor leitura de ADX considerada uma tendência negociável. |
| Volume | 1 | Volume base da ordem, em lotes; o tamanho da posição aberta é somado a ele. |
| Candles | 00:15:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o bloco de indicador e três conversores retiram Dx.Plus, Dx.Minus e MovingAverage do seu valor.
- O bloco de cruzamento emite verdadeiro quando +DI sobe acima de -DI e falso quando cai abaixo, então um NÃO lógico transforma a mesma saída no sinal de venda.
- Uma comparação testa a linha ADX contra a constante de limiar; outras duas comparam a posição com zero, uma por lado.
- Cada E lógico une o cruzamento, o filtro de tendência e a verificação de posição e aciona um bloco de modificação de posição cujo volume vem do bloco de fórmula.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
