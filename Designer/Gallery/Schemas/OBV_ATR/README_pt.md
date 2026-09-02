# Diagrama da estratégia de rompimento de canal do OBV
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

O On-Balance Volume soma o volume de cada candle de alta e subtrai o de cada candle de baixa, então sua curva é o saldo acumulado da pressão compradora contra a vendedora. Este diagrama coloca um canal ao estilo Donchian sobre essa curva, e não sobre o preço: quando o OBV sai por cima do canal dos candles anteriores, a acumulação assumiu e o esquema compra; quando sai por baixo, a distribuição assumiu e o esquema vende.

![schema](schema.svg)

## Visão geral da estratégia

- O canal é formado por um bloco Highest e um Lowest de 60 valores, alimentados pelo bloco On-Balance Volume e não pelos candles.
- Dois blocos de valor anterior guardam o canal do candle precedente, de modo que o rompimento é medido contra uma borda que o valor atual do OBV ainda não deslocou.
- Como a borda vem do candle anterior, o rompimento é um evento e não um estado: negocia exatamente o candle que empurra o OBV além do extremo antigo.
- A estratégia original leva ATR no nome, mas seu próprio código nunca usa esse indicador, então o diagrama o deixa de fora e mantém só o que de fato decide uma operação.

## Regras de entrada e saída

- **Entrada comprada**: O valor atual do OBV está acima do topo do canal do candle anterior e a posição não está comprada. A ordem compra um lote: a partir do zero abre uma compra, a partir de uma venda a encerra.
- **Entrada vendida**: O valor atual do OBV está abaixo do fundo do canal do candle anterior e a posição não está vendida. A ordem vende um lote: a partir do zero abre uma venda, a partir de uma compra a encerra.
- **Saída**: O bloco de proteção encerra a operação com take profit de 5 por cento ou stop loss de 3 por cento sobre o preço de entrada; o rompimento contrário também zera a posição, pois todas as ordens usam o mesmo volume.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Channel Length | 60 | Quantidade de valores do OBV na janela de Highest e Lowest; os dois blocos recebem o mesmo comprimento. |
| Take profit, % | 5 | Distância do take profit em relação ao preço de entrada, em porcentagem. |
| Stop loss, % | 3 | Distância do stop loss em relação ao preço de entrada, em porcentagem. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. |

## Detalhes do diagrama

- O bloco de candles alimenta o bloco On-Balance Volume, cuja saída segue para os blocos Highest e Lowest: um indicador lendo outro indicador.
- Cada borda do canal passa por um bloco de valor anterior, de modo que a comparação usa a borda do candle anterior ao rompimento.
- Dois blocos de comparação medem o OBV atual contra essas bordas e outros dois comparam a posição com uma constante zero; cada E lógico une um rompimento à sua verificação de posição.
- O original mantém um regime de alta ou de baixa que gruda e só negocia quando ele vira; no diagrama a verificação de posição produz a mesma entrada única por movimento, barrando um rompimento repetido no sentido já posicionado.
- Os dois blocos de modificação enviam ordens a mercado com o volume de uma constante compartilhada, e seus negócios alimentam o bloco de proteção com o take profit e o stop loss.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
