# Diagrama da estratégia de cruzamento de níveis do MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O Money Flow Index pondera cada movimento de preço pelo volume que o acompanha, então mostra quanto dinheiro realmente empurra o mercado. Este diagrama opera contra os extremos: compra no candle em que o MFI desce atravessando o nível inferior para a zona de sobrevenda e vende no candle em que ele sobe atravessando o nível superior para a zona de sobrecompra. Um take profit e um stop loss percentuais encerram cada operação.

![schema](schema.svg)

## Visão geral da estratégia

- O Money Flow Index de período 14 é calculado sobre candles horários finalizados, que o testador monta a partir do histórico de cinco minutos incluído.
- Os níveis 30 e 70 são lidos como cruzamentos e não como zonas: apenas o candle que entra em uma zona gera sinal, não os que permanecem dentro dela.
- A estratégia original tem um seletor Trend capaz de espelhar os dois sinais; o diagrama mantém o modo Direct padrão, de modo que entrar na sobrevenda compra e entrar na sobrecompra vende.
- A posição atual participa das duas decisões, então o esquema nunca soma uma segunda ordem a uma posição já aberta.

## Regras de entrada e saída

- **Entrada comprada**: O valor anterior do MFI estava acima do nível inferior e o atual está nele ou abaixo, e a posição não está comprada. A ordem compra um lote: a partir do zero abre uma compra, a partir de uma venda a encerra.
- **Entrada vendida**: O valor anterior do MFI estava abaixo do nível superior e o atual está nele ou acima, e a posição não está vendida. A ordem vende um lote: a partir do zero abre uma venda, a partir de uma compra a encerra.
- **Saída**: O bloco de proteção encerra a operação com take profit de 2 por cento ou stop loss de 1 por cento sobre o preço de entrada; antes disso, o cruzamento contrário zera a posição, pois todas as ordens usam o mesmo volume.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| MFI Length | 14 | Período de suavização do Money Flow Index. |
| Low Level | 30 | Nível que o indicador precisa atravessar para baixo para liberar uma compra. |
| High Level | 70 | Nível que o indicador precisa atravessar para cima para liberar uma venda. |
| Take profit, % | 2 | Distância do take profit em relação ao preço de entrada, em porcentagem. |
| Stop loss, % | 1 | Distância do stop loss em relação ao preço de entrada, em porcentagem. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 01:00:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o bloco de indicador com o Money Flow Index, e um bloco de valor anterior guarda a leitura de um candle atrás.
- Quatro blocos de comparação montam os dois cruzamentos: anterior acima do nível mais atual nele ou abaixo para o lado comprado; anterior abaixo mais atual nele ou acima para o lado vendido.
- Outros dois blocos de comparação testam a posição contra uma constante zero, e cada E lógico une um cruzamento à sua verificação de posição.
- Os dois blocos de modificação enviam ordens a mercado com o volume de uma constante compartilhada, e seus negócios alimentam o bloco de proteção com o take profit e o stop loss.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
