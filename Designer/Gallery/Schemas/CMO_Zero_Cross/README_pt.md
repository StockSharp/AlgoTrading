# Diagrama da estratégia de cruzamento do zero pelo CMO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Chande Momentum Oscillator oscila entre -100 e +100 e troca de sinal exatamente quando a pressão compradora e a vendedora trocam de lugar. Este diagrama negocia essa troca de sinal, mas somente quando a nova leitura já está longe o bastante do zero para justificar uma ordem, ignorando o vaivém plano em torno da linha zero.

![schema](schema.svg)

## Visão geral da estratégia

- O Chande Momentum Oscillator é calculado sobre candles horários finalizados de um único instrumento.
- O cruzamento é lido a partir de dois valores, o oscilador um candle atrás e o atual, em vez de um bloco de cruzamento, o que deixa a direção do movimento explícita no desenho.
- Um filtro de força exige que a nova leitura se afaste do zero pelo menos uma distância mínima, descartando os cruzamentos rasos que ocorrem quando o mercado está parado.
- A posição participa de cada decisão e ainda define o tamanho da ordem, de modo que um sinal contrário a uma operação aberta a inverte com uma única ordem a mercado.

## Regras de entrada e saída

- **Entrada comprada**: O oscilador estava abaixo de zero no candle anterior e agora está no nível positivo mínimo ou acima dele, e a posição não está comprada. A ordem compra o volume compartilhado mais o tamanho de uma venda aberta, de modo que uma única ordem a mercado encerra a venda e abre a compra.
- **Entrada vendida**: O oscilador estava em zero ou acima no candle anterior e agora está no nível negativo mínimo ou abaixo dele, e a posição não está vendida. A ordem vende o volume compartilhado mais o tamanho de uma compra aberta.
- **Saída**: Não há bloco de saída próprio: a posição é deixada pelo cruzamento contrário do zero, que a inverte, ou pelo bloco de proteção. O original usa take profit absoluto de 2000 e stop loss de 1000 passos de preço; níveis absolutos calibrados para outro instrumento jamais seriam alcançados neste histórico, então aqui eles aparecem como alvo de dois por cento e stop de um por cento, mantendo a proporção de dois para um. O original também faz uma pausa de quatro candles após cada mudança de posição; não existe bloco que guarde um contador de barras entre candles, portanto a pausa foi removida e a verificação de posição sozinha impede uma segunda entrada no mesmo sentido.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| CMO Length | 14 | Período de suavização do Chande Momentum Oscillator. |
| Min |CMO| | 5 | Distância mínima do zero que o oscilador precisa alcançar para o cruzamento valer. |
| Volume | 1 | Volume da ordem, em lotes. |
| Take profit, % | 2 | Distância do take profit em relação ao preço de entrada, em porcentagem. |
| Stop loss, % | 1 | Distância do stop loss em relação ao preço de entrada, em porcentagem. |
| Candles | 01:00:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o bloco de indicador com o Chande Momentum Oscillator e um conversor que pega o preço de fechamento para o bloco de proteção.
- Um bloco de valor anterior guarda o oscilador de um candle atrás, e dois blocos de comparação decidem de que lado do zero ele estava.
- A constante de força entra diretamente na comparação da compra e, por meio de uma pequena fórmula que a nega, na comparação da venda, de modo que um único parâmetro comanda os dois lados.
- Cada E lógico une o lado anterior, o filtro de força e a verificação de posição e aciona um bloco de modificação de posição cujo volume vem da fórmula que soma a posição absoluta ao volume compartilhado.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
