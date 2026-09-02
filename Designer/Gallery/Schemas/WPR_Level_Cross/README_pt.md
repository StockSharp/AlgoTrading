# Diagrama da estratégia de cruzamento de níveis do Williams %R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Williams %R mostra onde o fechamento está dentro da faixa dos últimos candles, de 0 no topo a -100 no fundo. Este diagrama negocia o instante em que o oscilador entra em uma zona, e não aquele em que sai dela: uma queda através do nível inferior compra e uma alta através do nível superior vende. A proteção percentual encerra a operação.

![schema](schema.svg)

## Visão geral da estratégia

- O Williams %R de período 14 é calculado sobre candles horários finalizados, que o testador monta a partir do histórico de cinco minutos incluído.
- O sinal é o próprio cruzamento: a leitura anterior de um lado do nível e a atual do outro, de modo que uma permanência longa dentro da zona dispara apenas uma vez.
- Trata-se da entrada na zona, o espelho da leitura clássica que espera o oscilador voltar para fora, e corresponde ao modo Direct da estratégia original.
- O original ainda traz permissões separadas para compras e vendas; ambas ligadas por padrão, então o diagrama liga os dois lados e basta desconectar um ramo para desativar um deles.

## Regras de entrada e saída

- **Entrada comprada**: O %R anterior estava acima do nível inferior e o atual está nele ou abaixo, e a posição não está comprada. A ordem compra um lote: a partir do zero abre uma compra, a partir de uma venda a encerra.
- **Entrada vendida**: O %R anterior estava abaixo do nível superior e o atual está nele ou acima, e a posição não está vendida. A ordem vende um lote: a partir do zero abre uma venda, a partir de uma compra a encerra.
- **Saída**: O bloco de proteção encerra a operação com take profit de 2 por cento ou stop loss de 1 por cento sobre o preço de entrada; antes disso, o cruzamento contrário zera a posição, pois todas as ordens usam o mesmo volume.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Williams %R Length | 14 | Comprimento de cálculo do Williams %R. |
| Low Level | -80 | Nível que o oscilador precisa atravessar para baixo para liberar uma compra. |
| High Level | -20 | Nível que o oscilador precisa atravessar para cima para liberar uma venda. |
| Take profit, % | 2 | Distância do take profit em relação ao preço de entrada, em porcentagem. |
| Stop loss, % | 1 | Distância do stop loss em relação ao preço de entrada, em porcentagem. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 01:00:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o bloco de indicador com o Williams %R, e um bloco de valor anterior guarda a leitura de um candle atrás.
- Quatro blocos de comparação constroem os dois cruzamentos com a leitura anterior e a atual diante das duas constantes de nível.
- Outros dois blocos de comparação testam a posição contra uma constante zero, e cada E lógico une um cruzamento à sua verificação de posição.
- Os dois blocos de modificação enviam ordens a mercado com o volume de uma constante compartilhada, e seus negócios alimentam o bloco de proteção com o take profit e o stop loss.
- O original protege com distâncias absolutas de preço; o diagrama usa porcentagens do preço de entrada, para que os mesmos números sirvam em qualquer instrumento.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
