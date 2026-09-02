# Diagrama da estratégia de reversão de três candles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Dois candles empurram o mercado para baixo, o segundo marcando uma mínima menor que a do primeiro, e então um terceiro vira e fecha acima da máxima do segundo. Essa sequência diz que os vendedores gastaram o último empurrão e foram respondidos por inteiro, e o diagrama compra isso. A figura espelhada é vendida. Depois, uma média móvel simples do preço de fechamento conduz a operação e decide quando ela acabou.

![schema](schema.svg)

## Visão geral da estratégia

- Dois blocos de padrão de candles carregam, cada um, uma fórmula de três candles, então a figura inteira é reconhecida em um bloco em vez de uma parede de comparações.
- A fórmula comprada pede um candle de baixa, depois um candle de baixa com mínima inferior e, em seguida, um candle de alta fechando acima da máxima do candle do meio.
- A fórmula vendida é o espelho exato: alta, alta com máxima superior e depois baixa fechando abaixo da mínima do candle do meio.
- A média móvel simples não participa da entrada: é apenas a linha em que a operação é abandonada, exatamente como na estratégia original.

## Regras de entrada e saída

- **Entrada comprada**: O bloco do padrão de alta informa a reversão de três candles concluída e a posição está zerada. A ordem compra um lote e abre uma compra.
- **Entrada vendida**: O bloco do padrão de baixa informa a reversão espelhada concluída e a posição está zerada. A ordem vende um lote e abre uma venda.
- **Saída**: A compra é encerrada quando um candle fecha abaixo da média móvel e a venda quando fecha acima, ambas por blocos de modificação de posição em modo de fechamento, exatamente como no original. O código original não tem stop nem alvo, então o diagrama também não tem. Ficou de fora a pausa de várias centenas de candles que o original mantém após cada operação: um contador de barras só se monta devolvendo um sinal ao próprio diagrama, o que fecharia o grafo em um laço, então aqui todo padrão visto é operado. Por isso a frequência de negócios é bem maior que a do original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| SMA Length | 20 | Período de suavização da média móvel simples que encerra as operações. |
| Volume | 1 | Volume da ordem, em lotes. |
| Candles | 00:05:00 | Tempo gráfico dos candles com que todo o diagrama trabalha. A estratégia original usa candles de um minuto; aqui são cinco minutos, para casar com o histórico incluído e manter a figura legível. |

## Detalhes do diagrama

- O bloco de candles alimenta quatro ramos: os dois blocos de padrão, a média móvel e um conversor que extrai o preço de fechamento do candle.
- Cada bloco de padrão traz três fórmulas, uma por candle da figura, e responde verdadeiro apenas no candle que a completa; os valores com prefixo p leem o candle anterior.
- O bloco de posição é comparado com uma constante zero e essa única verificação protege as duas entradas, de modo que um padrão gera uma operação.
- Os dois blocos de entrada enviam ordens a mercado e tiram o volume de uma constante compartilhada; os dois blocos de saída são acionados diretamente pelas comparações com a média e só agem quando há algo a encerrar.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
