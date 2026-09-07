# Diagrama de estratégia de cruzamento de médias móveis entre períodos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este diagrama combina uma média móvel simples de 10 períodos calculada com candles finalizados de quatro horas e uma média móvel simples de 40 períodos calculada com candles finalizados de uma hora. As duas configurações representam uma janela nominal de 40 horas. O último valor formado da média do período superior é armazenado e combinado com a média base atual em cada candle base finalizado; a direção do cruzamento e a posição atual encaminham ações a mercado de 0.1 fixo, incluindo reversões em duas etapas confirmadas pela execução. O gráfico mostra candles de uma hora, as duas médias sincronizadas e quatro fluxos de execuções.

![schema](schema.svg)

## Visão geral da estratégia

- Candles finalizados de quatro horas alimentam Higher SMA 10, enquanto candles finalizados de uma hora alimentam Base SMA 40 e acionam o ciclo de decisão. Com as configurações padrão, cada média cobre 40 horas nominais: 10 × 4 horas e 40 × 1 hora.
- O último valor formado de Higher SMA é armazenado. Em cada candle finalizado de uma hora, o diagrama atualiza esse valor e a Base SMA atual; em seguida, um bloco Sync com intervalo de uma hora e o candle base como âncora libera os valores alinhados em conjunto.
- Um único bloco Crossing emite `true` quando Higher SMA cruza Base SMA para cima e `false` quando cruza para baixo. Um bloco NOT transforma o evento de baixa em um acionador positivo para o caminho vendido.
- A posição atual separa cada cruzamento nos casos sem posição, posição comprada e posição vendida. Sem posição, abre-se 0.1 na direção do sinal; uma posição já alinhada não é alterada; uma posição contrária inicia uma reversão em etapas.
- A reversão em etapas envia primeiro uma ação a mercado ReduceOnly de 0.1. Somente a Order de fechamento totalmente executada aciona uma ação a mercado NoCondition fixa de 0.1 na nova direção. A sequência é dimensionada para uma posição criada pelo diagrama com o mesmo Order Volume; um tamanho real diferente pode não terminar na exposição desejada. Não há blocos de stop-loss nem take-profit.

## Regras de entrada e saída

- **Entrada comprada**: Quando Crossing emite um evento de alta, o ramo de posição zerada filtrado externamente envia uma compra a mercado NoCondition de Order Volume 0.1. Uma posição vendida envia primeiro uma compra a mercado ReduceOnly de 0.1; somente sua Order totalmente executada aciona a segunda compra NoCondition de 0.1. Uma posição comprada existente não é alterada.
- **Entrada vendida**: Quando Crossing emite um evento de baixa, NOT ativa o caminho vendido. O ramo de posição zerada filtrado externamente envia uma venda a mercado NoCondition de Order Volume 0.1. Uma posição comprada envia primeiro uma venda a mercado ReduceOnly de 0.1; somente sua Order totalmente executada aciona a segunda venda NoCondition de 0.1. Uma posição vendida existente não é alterada.
- **Saída**: Não há regra independente de saída, stop-loss ou take-profit. Um cruzamento válido na direção contrária executa a sequência de fechamento e abertura com quantidade fixa. O primeiro passo ReduceOnly não pode aumentar nem reverter a exposição, mas a ação NoCondition seguinte não é redimensionada para uma posição externa; se o tamanho real for diferente de Order Volume, a posição final desejada não é garantida.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|---|---|---|
| Higher Candles Series | 04:00:00 | Série de candles de quatro horas usada por Higher SMA. Somente candles finalizados atualizam a média armazenada do período superior. |
| Base Candles Series | 01:00:00 | Série de candles de uma hora usada por Base SMA. Cada candle finalizado ancora uma avaliação sincronizada e também é desenhado no gráfico. |
| Higher SMA Length | 10 | Período da média móvel simples calculada em candles de quatro horas. Dez candles representam uma janela nominal de 40 horas. |
| Higher SMA Source | unset | Mantido sem configuração; por isso, Higher SMA lê o preço Close de cada candle finalizado de quatro horas. |
| Base SMA Length | 40 | Período da média móvel simples calculada em candles de uma hora. Quarenta candles representam a mesma janela nominal de 40 horas. |
| Base SMA Source | unset | Mantido sem configuração; por isso, Base SMA lê o preço Close de cada candle finalizado de uma hora. |
| Order Volume | 0.1 | Quantidade fixa usada em entradas sem posição, fechamentos ReduceOnly e na segunda etapa da reversão confirmada pela execução. |

## Detalhes do diagrama

- Dois blocos [Candles](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emitem somente candles finalizados de quatro horas e uma hora. Dois blocos de [Indicador](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) independentes e limitados a valores formados calculam SimpleMovingAverage 10 e SimpleMovingAverage 40 pelos preços Close.
- Um bloco [Variável](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) armazena o último valor formado de Higher SMA. Cada candle base finalizado atualiza esse valor e Base SMA antes de entrar em [Sync](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/sync.html), cujo Interval é `01:00:00`, ClearSockets está ativado e a entrada de candle fornece a âncora horária.
- As saídas numéricas sincronizadas entram em um único bloco [Cruzamento](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/crossing.html). Seu evento de alta `true` conduz ao caminho comprado, enquanto uma [Condição lógica](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) NOT transforma o evento de baixa `false` em um acionador positivo de venda.
- A [Posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/current.html) atual é atualizada no mesmo ciclo do candle base. Blocos de [Comparação](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) distinguem `Position = 0`, `Position > 0` e `Position < 0`, portanto uma posição já alinhada não recebe outra entrada.
- Em um evento de alta, o caminho sem posição filtrado externamente chama um bloco de compra [Modificar posição](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) com NoCondition. O caminho vendido chama primeiro uma compra ReduceOnly; sua saída Order só aparece após a execução completa do fechamento e então prepara a compra NoCondition fixa.
- O caminho de baixa é simétrico: o caminho sem posição filtrado externamente abre uma venda com NoCondition, enquanto uma posição comprada é reduzida por uma venda antes que sua Order totalmente executada prepare a venda NoCondition fixa. As quatro ações usam MarketOrder e Order Volume 0.1. Não há blocos de stop-loss, take-profit nem de saída por tempo.
- O [Painel de gráfico](https://doc.stocksharp.com/pt/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recebe candles finalizados de uma hora, os valores sincronizados de Higher SMA e Base SMA e as saídas MyTrade das ações de abertura comprada, abertura vendida, fechamento de venda e fechamento de compra.

## Uso

Importe o arquivo `.json` no Designer, execute-o sobre dados históricos no backtester e depois ajuste os parâmetros ou os próprios blocos ao seu instrumento antes de operar ao vivo.
