# Diagrama da estratégia de votação SMA multi-timeframe
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma média móvel virando para cima diz pouco; três delas virando para cima juntas, em três timeframes diferentes, já são uma tendência que vale operar. Cada timeframe emite um voto de mais um, menos um ou zero, o Sync reúne os três votos em um único conjunto, e uma posição só é aberta quando o voto é unânime.

![schema](schema.svg)

## Visão geral da estratégia

- Três blocos de candles leem o mesmo instrumento em cinco minutos, quinze minutos e uma hora, e cada um deles repassa somente candles finalizados.
- Cada série alimenta a sua própria média móvel simples, e um bloco Previous value guarda essa mesma média como ela estava uma barra antes.
- Uma fórmula transforma o par em um voto: mais um quando a média está acima de onde estava, menos um quando está abaixo, zero quando não se moveu.
- Os votos de cinco e de quinze minutos ficam retidos em variáveis e são liberados pelo voto horário, de modo que as três linhas chegam ao Sync pertencendo a um único e mesmo momento.
- O Sync mantém uma linha por voto e libera as três juntas; sem ele, a leitura horária chegaria uma hora depois da de cinco minutos e as três nunca poderiam ser comparadas.
- Depois do Sync, cada voto é comparado com zero, e duas condições lógicas fazem a única pergunta que interessa ao diagrama: os três apontam para o mesmo lado?
- O veredito é travado no próximo candle de cinco minutos finalizado, que é o compasso pelo qual toda ordem é datada, e o Position modify abre a mercado a partir de posição zerada.
- A posição, amostrada nesse mesmo compasso de cinco minutos, é comparada com zero: o veredito oposto contra uma posição aberta a fecha a mercado, e o novo lado é assumido no candle seguinte.

## Regras de entrada e saída

- **Entrada comprada**: Os três votos são mais um no compasso horário: as médias de cinco minutos, de quinze minutos e horária estão, cada uma, acima do seu próprio valor de uma barra atrás. O veredito é levado ao próximo candle de cinco minutos finalizado, onde o Position modify compra o volume da ordem a mercado, e somente a partir de posição zerada.
- **Entrada vendida**: Os três votos são menos um no mesmo compasso: cada média está abaixo do seu próprio valor de uma barra atrás. No próximo candle de cinco minutos finalizado, o Position modify vende o volume da ordem a mercado, novamente somente a partir de posição zerada.
- **Saída**: Não há take-profit, stop-loss nem temporizador. A posição vive até que os três timeframes se alinhem para o outro lado: o veredito oposto, junto com a comparação da posição, a fecha a mercado no candle de negociação, e a entrada do novo lado vem no candle seguinte, quando a posição estiver zerada de novo. Um voto misto não muda nada e deixa a posição como está.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Fast Candles | 00:05:00 | Timeframe do voto mais rápido e compasso no qual os vereditos são executados e as ordens são datadas. |
| Medium Candles | 00:15:00 | Timeframe do voto intermediário. |
| Slow Candles | 01:00:00 | Timeframe do voto mais lento e compasso no qual os três votos são reunidos e contados. |
| Fast SMA Length | 13 | Período de média da média de cinco minutos. |
| Medium SMA Length | 13 | Período de média da média de quinze minutos. |
| Slow SMA Length | 13 | Período de média da média horária. |
| Order Volume | 1 | Tamanho da ordem, em lotes, usado nos dois lados de entrada; a saída fecha o que quer que a posição contenha. |

## Detalhes do diagrama

- As três médias emitem apenas valores formados e finais, de modo que um candle ainda em formação nunca consegue mexer em um voto.
- Reduzir cada timeframe ao sinal da sua inclinação é o que torna os três comparáveis: uma média horária e uma de cinco minutos vivem em escalas diferentes, mas mais um e menos um não.
- Os dois votos mais rápidos entram no Sync por meio de uma variável liberada pelo voto horário. Isso é proposital: linhas que chegam uma de cada vez deixariam o Sync segurando um conjunto que nunca se completa, e nada depois dele voltaria a disparar.
- O Sync carimba o conjunto que libera com o momento a que esse conjunto pertence, momento que já está atrasado em relação ao relógio quando a hora fecha. Nada no caminho até a ordem lê esse carimbo - o veredito é travado uma segunda vez no candle de cinco minutos finalizado que carrega a operação.
- Ambas as entradas são condicionadas à posição aberta, de modo que um veredito repetido a cada candle de cinco minutos não consegue empilhar ordens: enquanto a posição vive, a repetição é simplesmente recusada.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
