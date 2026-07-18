# Estratégia de Médias Móveis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A estratégia de Médias Móveis replica o expert clássico do MetaTrader que negocia cruzamentos do preço em relação a uma média móvel simples (SMA) deslocada. O algoritmo processa apenas candles concluídos, garantindo que todas as decisões de trading sejam baseadas em barras completamente formadas. O dimensionamento de posição segue um modelo de risco dinâmico vinculado ao capital da conta e se adapta a sequências de perdas, imitando a implementação MQL original.

## Lógica de Trading
- Uma média móvel simples é calculada com um período configurável e um deslocamento adicional para a frente medido em barras concluídas.
- Em cada candle concluído, a estratégia verifica se a barra abriu acima da SMA deslocada e fechou abaixo dela (cruzamento baixista) ou abriu abaixo e fechou acima (cruzamento altista).
- O sistema gerencia apenas uma posição por vez. Quando ocorre um cruzamento contra a posição ativa, a posição é fechada primeiro e nenhuma ordem de reversão é enviada na mesma barra.
- Se nenhuma posição estiver aberta:
  - Um cruzamento altista abre uma posição comprada.
  - Um cruzamento baixista abre uma posição vendida.

## Gestão de Posições
- Posições compradas são fechadas quando ocorre um cruzamento baixista.
- Posições vendidas são fechadas quando ocorre um cruzamento altista.
- A execução de negociações usa ordens de mercado no instrumento da estratégia.
- O histórico de negociações é rastreado para calcular o preço de entrada efetivo para que lucros e perdas possam ser medidos ao fechar a posição.

## Gestão de Risco e Dimensionamento de Posição
- O volume base da ordem é derivado do capital do portfólio multiplicado pelo parâmetro **Maximum Risk**, dividido pelo preço de fechamento atual. Se o capital não estiver disponível, a estratégia usa o volume padrão da estratégia.
- Um parâmetro **Decrease Factor** reduz o volume de ordem calculado quando são detectadas negociações consecutivas perdedoras. A redução é proporcional à sequência de perdas, reproduzindo a lógica de dimensionamento adaptativo da versão MQL.
- O volume da ordem nunca é negativo; quando a redução compensa completamente o valor base, a negociação é ignorada.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `MaximumRisk` | Fração do capital da conta arriscada em cada negociação. | `0.02` |
| `DecreaseFactor` | Divisor usado para reduzir o volume após perdas consecutivas. | `3` |
| `MovingPeriod` | Período da SMA usada para sinais. | `12` |
| `MovingShift` | Número de barras concluídas usadas para deslocar a SMA para a frente. | `6` |
| `CandleType` | Série de candles usada para cálculos (período). | Candles `5m` |

## Notas
- O deslocamento da média móvel é implementado por meio de um buffer circular interno para que a estratégia use o valor da SMA de várias barras atrás, assim como o parâmetro de deslocamento do indicador do MetaTrader.
- As ordens são geradas apenas quando tanto a SMA quanto o buffer deslocado estão completamente formados, prevenindo negociações prematuras durante o aquecimento.
- Mensagens de log documentam entradas, saídas e resultados de negociações para auxiliar na depuração e análise de desempenho.
