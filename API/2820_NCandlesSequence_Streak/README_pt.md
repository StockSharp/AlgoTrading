# Estratégia de Sequência de N Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de Sequência de N Velas replica o comportamento do assessor especialista MetaTrader original "N-_Candles_v7" usando a API de alto nível do StockSharp. Ela monitora velas finalizadas e procura um número configurável de corpos consecutivos de alta ou baixa. Quando uma sequência qualificada está presente, a estratégia abre uma posição na mesma direção e a gerencia com take profit, stop loss, trailing stop, filtro de horas de trading e bloqueio de lucro flutuante configuráveis.

## Lógica de trading
- Avalia cada vela finalizada e a classifica como de alta, baixa ou neutra (doji). Velas neutras reiniciam o contador de sequência e podem acionar o comportamento de "ovelha negra".
- Mantém uma contagem contínua de velas consecutivas com a mesma direção de corpo. Uma vez que a contagem atinge o limite configurado, a direção atual se torna o padrão ativo.
- Quando uma sequência de alta está ativa, a estratégia tenta abrir uma posição comprada; quando uma sequência de baixa está ativa, tenta abrir uma posição vendida. Apenas uma posição líquida é mantida de cada vez.
- Se uma vela quebrar a direção uniforme ("ovelha negra"), a estratégia reage de acordo com o modo de fechamento selecionado: fechar tudo, fechar apenas posições opostas, ou fechar apenas posições alinhadas com a sequência anterior.
- Opcionalmente, restringe entradas a uma janela de trading definida por horas de início e fim (inclusivas).
- Monitora continuamente a posição aberta para take profit, stop loss, atualizações de trailing stop e o limite de lucro flutuante.

## Gerenciamento de posição e risco
- O stop protetor inicial e o alvo são calculados a partir de distâncias em pips convertidas com o passo de preço do instrumento. Esses níveis são recalculados cada vez que uma nova posição é aberta.
- A lógica do trailing stop imita o assessor original: uma vez que o preço percorre a distância de trailing mais o passo, o stop é movido para manter a distância de trailing.
- Um guardião de lucro flutuante (`MinProfit`) fecha toda a posição assim que o lucro aberto excede o valor configurado.
- O parâmetro `MaxPositionVolume` evita entradas se o volume solicitado estiver acima do limite permitido. `MaxPositions` funciona como proteção contra re-entrada quando uma posição já está ativa.
- Todas as saídas chamam ordens a mercado para achatar a posição líquida porque a estratégia do StockSharp opera em um ambiente de netting.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `ConsecutiveCandles` | Número de velas com direção idêntica necessárias para acionar um sinal. |
| `OrderVolume` | Volume de ordem a mercado usado para entradas e saídas. |
| `TakeProfitPips` | Distância de take profit expressa em pips. Definir como zero para desabilitar. |
| `StopLossPips` | Distância de stop loss expressa em pips. Definir como zero para desabilitar. |
| `TrailingStopPips` | Distância do trailing stop. Definir como zero para desabilitar o trailing. |
| `TrailingStepPips` | Distância adicional necessária antes que o trailing stop seja movido. |
| `MaxPositions` | Número máximo de entradas simultâneas por padrão (a estratégia mantém uma única posição líquida). |
| `MaxPositionVolume` | Limite superior para o volume líquido permitido. |
| `UseTradeHours` / `StartHour` / `EndHour` | Habilitar e configurar a janela de tempo de trading (inclusiva). |
| `MinProfit` | Limite de lucro flutuante que aciona uma saída completa. |
| `ClosingBehavior` | Define como reagir quando uma vela de "ovelha negra" aparece. |
| `CandleType` | A série de velas usada para cálculos. |

## Notas e premissas
- A estratégia opera com posições líquidas; múltiplos tickets de estilo hedging não são criados. Isso difere do assessor original onde várias posições cobertas podiam coexistir.
- O lucro flutuante é aproximado como `(preço atual - preço de entrada) * volume` para posições compradas e o inverso para posições vendidas.
- A conversão de pips depende do `PriceStep` do instrumento. Para símbolos onde o passo mínimo não é fornecido, um pip padrão de 0.0001 é assumido.
- Nenhuma portação para Python é fornecida, conforme solicitado.
