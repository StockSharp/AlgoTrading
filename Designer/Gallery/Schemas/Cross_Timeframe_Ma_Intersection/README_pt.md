# Diagrama de cruzamento de médias com sincronização
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama sincroniza um candle horário com os valores completos de uma média exponencial rápida e outra lenta antes de avaliar o cruzamento. Ele abre uma unidade no primeiro sinal e usa duas vezes o volume-base em cada sinal oposto, invertendo a posição sem aumentar seu tamanho.

![schema](schema.svg)

## Visão geral da estratégia

- Uma única série de candles horários finalizados alimenta EMA(20) e EMA(50), mantendo a mesma base de preços para as duas médias.
- O bloco Sync forma um grupo horário com candle, EMA rápida e EMA lenta; nenhuma decisão de cruzamento é tomada com um grupo incompleto.
- Crossing emite `true` quando a EMA rápida sobe acima da lenta e `false` quando cai abaixo; um bloco NOT transforma o segundo valor no disparo vendido.
- As verificações do sinal da posição escolhem uma abertura com volume-base a partir do zero ou uma inversão com duas vezes o volume-base a partir do lado oposto.
- O gráfico recebe o candle sincronizado, os dois valores EMA sincronizados e todas as execuções da estratégia.

## Regras de entrada e saída

- **Entrada comprada**: Quando a EMA(20) sincronizada cruza acima da EMA(50), compra-se um volume-base a partir do zero ou dois volumes-base a partir de uma posição vendida, restando uma posição comprada de um volume-base.
- **Entrada vendida**: Quando a EMA(20) sincronizada cruza abaixo da EMA(50), vende-se um volume-base a partir do zero ou dois volumes-base a partir de uma posição comprada, restando uma posição vendida de um volume-base.
- **Saída**: Não há stop, alvo nem saída por tempo separados. O próximo cruzamento oposto envia uma inversão a mercado cujo volume fecha a unidade atual e abre uma unidade na nova direção.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Fast EMA Length | 20 | Comprimento da média móvel exponencial mais rápida. |
| Slow EMA Length | 50 | Comprimento da média móvel exponencial mais lenta. |
| Candles | 01:00:00 | Período dos candles finalizados usados pelas duas médias. |
| Sync interval | 01:00:00 | Faixa de tempo usada pelo Sync para agrupar o candle e os dois valores de indicadores. |
| Base volume | 1 | Tamanho aberto a partir do zero; uma inversão usa automaticamente o dobro deste valor. |

## Detalhes do diagrama

- A saída dos candles primeiro atualiza o retrato da posição e as constantes de zero e volume, depois alimenta EMA(20) e EMA(50); sua conexão final entra no terceiro input do Sync e completa o grupo após os dois cálculos.
- Sync Input 1 recebe EMA(20), Input 2 recebe EMA(50) e Input 3 recebe o candle. As três saídas pareadas estão conectadas, e o bloco limpa cada grupo completo.
- Sync Output 1 e Output 2 alimentam Crossing Input Up e Input Down. Output 3 passa por um conversor ClosePrice e também fornece a série de candles ao gráfico.
- Quatro rotas lógicas distinguem os estados zerado, comprado e vendido nas duas direções de cruzamento. Um sinal do mesmo lado não pode aumentar uma posição existente.
- Quatro blocos Modify position enviam ordens a mercado: duas aberturas usam o volume-base e duas inversões usam a fórmula `2 × volume-base`.
- Como cada posição abre com o volume-base, o valor da inversão equivale a `abs(position) + volume-base` e mantém constante o módulo da exposição.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
