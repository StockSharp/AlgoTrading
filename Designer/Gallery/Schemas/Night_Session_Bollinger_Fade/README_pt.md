# Diagrama da estratégia de reversão nas Bandas de Bollinger na sessão noturna
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A banda mostra onde o preço deixou de se comportar de forma habitual; o relógio mostra quando vale a pena agir a respeito disso. Este diagrama junta os dois em candles de uma hora já finalizados: ele opera contra o toque em uma banda de Bollinger, mas somente durante as horas da noite e somente enquanto o próprio canal estiver estreito. A saída é a linha média, e essa metade não é limitada pelo relógio - uma posição aberta no fim da noite é encerrada no momento em que o preço retorna, seja qual for a hora.

![schema](schema.svg)

## Visão geral da estratégia

- Uma única série de candles comanda tudo. Somente candles de uma hora finalizados são emitidos, de modo que cada comparação, cada filtro e cada ordem é decidido em uma barra fechada.
- As Bandas de Bollinger sobre 20 candles com desvio de 2.0 só emitem depois de formadas. Três blocos Converter separam o valor do indicador na banda superior, na banda inferior e na linha média.
- Um bloco Formula subtrai a banda inferior da superior para obter a largura do canal, e um bloco Comparison a confronta com o limite de largura. Essa única resposta é o filtro de mercado calmo compartilhado pelas duas entradas.
- O bloco Working time lê o timestamp que cada candle carrega - o seu horário de abertura - e responde true das 19:00:00 até 23:59:59. Apenas os dois filtros de entrada o consultam.
- Uma compra é aceita quando quatro respostas coincidem no mesmo candle: a mínima alcançou a banda inferior, o canal está dentro do limite, o candle abriu dentro da sessão e a posição está zerada.
- Uma venda é a imagem espelhada: a máxima alcançou a banda superior, sob as mesmas condições de largura, de sessão e de posição zerada.
- Ambas as entradas são ordens a mercado enviadas pelo bloco Modify position sob a condição Open position, de modo que um sinal que chega quando já existe posição aberta é recusado pelo próprio bloco de ordem, e não por uma comparação adicional.
- As saídas comparam o fechamento com a linha média e são enviadas como ordens Reduce only nas duas direções. O painel do gráfico desenha os candles, as três linhas das bandas e as ordens e execuções dos quatro blocos de ordem.

## Regras de entrada e saída

- **Entrada comprada**: Dentro da janela noturna, em um candle finalizado cuja mínima alcançou ou cruzou a banda inferior, com a largura do canal não maior que o limite e a posição zerada, o bloco Modify position compra o volume da ordem a mercado sob a condição Open position.
- **Entrada vendida**: Dentro da mesma janela, em um candle finalizado cuja máxima alcançou ou cruzou a banda superior, com a largura do canal não maior que o limite e a posição zerada, o bloco Modify position vende o volume da ordem a mercado sob a condição Open position.
- **Saída**: A linha média é o alvo dos dois lados. Em qualquer candle finalizado que feche nela ou acima dela, uma venda Reduce only é enviada; em qualquer candle que feche nela ou abaixo dela, uma compra Reduce only é enviada. Um bloco Reduce only que recebe a direção que a posição já mantém recusa a ordem por conta própria, de modo que o caminho de venda fica silencioso enquanto a posição está vendida e o caminho de compra fica silencioso enquanto ela está comprada. Nenhuma das saídas é limitada pela sessão nem pela largura do canal - elas funcionam em todo candle finalizado, a qualquer hora. Não há stop-loss nem take-profit: o retorno à linha média é a única forma de sair de uma operação.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Candles | 01:00:00 | Tempo gráfico da única série de candles sobre a qual todo o diagrama funciona; apenas candles finalizados saem dela. |
| Bollinger Period | 20 | Número de candles sobre os quais as bandas são calculadas pela média. |
| Bollinger Deviation | 2.0 | Multiplicador do desvio padrão que define a que distância da linha média ficam as duas bandas. |
| Width Threshold | 3000 | Canal mais largo, nas unidades de preço do instrumento, que ainda conta como calmo o bastante para entrar. |
| Session From | 19:00:00 | Início da janela em que as entradas são permitidas, comparado com o horário de abertura do candle. |
| Session Until | 23:59:59 | Fim dessa janela; um candle que abre nele ou antes dele ainda conta como dentro. |
| Order Volume | 0.01 | Tamanho da ordem enviado pelas duas entradas e pelas duas saídas. |

## Detalhes do diagrama

- A entrada lê os extremos do candle, e não o seu fechamento. Uma barra que perfurou uma banda durante a sua formação e fechou de volta para dentro ainda conta como toque, e é isso que faz desta uma reversão da excursão, e não um rompimento do preço de fechamento.
- O filtro de largura é medido nas unidades de preço do instrumento, não em porcentagem. Em um instrumento cotado em unidades, quase todo candle passa por ele e o filtro fica efetivamente aberto; em um cotado em dezenas de milhares, ele se torna o filtro seletivo que deveria ser. O limite fica exposto como parâmetro para que possa ser ajustado ao instrumento.
- Ambas as saídas carregam uma direção, ainda que o Reduce only decida o lado por si só: é a direção que faz cada caminho recusar a posição errada. Por isso nenhuma comparação de compra ou de venda aparece no lado das saídas do diagrama - são os dois blocos de ordem que executam esse teste.
- Uma única variável Volume alimenta os quatro blocos de ordem. Nas saídas, o Reduce only reduz a ordem ao que a posição realmente mantém, de modo que até mesmo uma entrada parcialmente executada é encerrada com exatidão, e não invertida.
- A sessão é avaliada por candle, e não a partir de um relógio de fluxo livre, de modo que ela muda de valor no mesmo compasso das respostas da banda e da largura, e as quatro entradas de um filtro de entrada sempre descrevem a mesma barra.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
