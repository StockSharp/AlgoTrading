# Estratégia da próxima barra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Nextbar** é uma tradução direta do MetaTrader 4 consultor especialista `nextbar.mq4`. O EA original avalia a distância entre a última vela concluída e uma vela várias barras mais antiga. Quando o preço viaja longe o suficiente em uma direção, ele segue o impulso ou negocia contra ele, dependendo da bandeira de direção configurada. As posições são então protegidas com níveis simétricos de take-profit/stop-loss e são fechadas à força após um número fixo de barras.

Esta versão StockSharp mantém o mesmo comportamento ao usar a estratégia de alto nível API. Ele processa apenas velas concluídas, garantindo que todos os cálculos correspondam à lógica de barra ao fechar do script MT4.

## Lógica MQL original
* **Distância do momento** – compare `Close[1]` com `Close[bars2check+1]`. Se a diferença for de pelo menos `minbar * Point`, trate-a como um sinal válido.
* **Sinalizador de direção** – a entrada MQL `direction` é igual a `1` para acompanhamento de tendência (comprar após uma alta, vender após uma queda) e `2` para negociação contrária (comprar após uma queda, vender após uma alta).
* **Restrição de entrada** – apenas um pedido pode ser aberto por vez. Uma nova negociação é enviada no início da barra seguindo o sinal.
* **Regras de saída** – feche uma posição comprada se o último fechamento atingir a distância de lucro acima da entrada ou a distância de perda abaixo dela; o inverso se aplica a shorts. Se nenhum dos níveis for atingido, feche a negociação após `bars2hold` velas concluídas.

## StockSharp destaques de implementação
* Usa `SubscribeCandles()` e `Bind` para receber velas concluídas no período configurado.
* Armazena um breve histórico contínuo de preços de fechamento para fazer referência à vela que corresponde ao deslocamento MQL `bars2check + 1`.
* Converte todos os parâmetros baseados em pontos com `Security.PriceStep`, imitando a constante MetaTrader `Point`.
* Coloca ordens de mercado com a estratégia `Volume` e suporta entradas de acompanhamento de impulso ou contrárias por meio do parâmetro `Direction`.
* Implementa saídas de lucros, perdas e períodos de retenção exatamente uma vez por vela concluída para permanecer alinhado com o fluxo de trabalho original.

## Parâmetros
| Parâmetro | Descrição | Padrão | Notas |
|-----------|-------------|---------|-------|
| `CandleType` | Prazo usado para avaliação do sinal. | Período de 1 hora | Anexe a estratégia a um título que possa fornecer esse tipo de vela. |
| `BarsToCheck` | Número de velas concluídas entre o fechamento de referência e o último fechamento. | 8 | Corresponde a `bars2check` de EA. |
| `BarsToHold` | Número máximo de velas concluídas para manter uma posição aberta. | 10 | Corresponde a `bars2hold`. A posição é fechada na barra onde o contador atinge este número. |
| `MinMovePoints` | Distância mínima (em MetaTrader pontos) entre os dois fechamentos comparados. | 77 | Corresponde a `minbar`. Convertido usando `Security.PriceStep`. |
| `TakeProfitPoints` | Distância alvo de lucro em MetaTrader pontos. | 115 | Equivalente à entrada `profit`. Defina como zero para desativar, se desejar. |
| `StopLossPoints` | Distância de stop-loss em MetaTrader pontos. | 115 | Equivalente à entrada `loss`. Defina como zero para desativar, se desejar. |
| `Direction` | Modo de negociação: `Follow` (tendência) ou `Reverse` (contrária). | `Follow` | Espelha a entrada `direction` (`1` = seguir, `2` = reverter). |
| `Volume` | Volume de negociação usado para ordens de mercado. | Volume de estratégia | Configure por meio da propriedade padrão `Strategy.Volume`. |

## Fluxo de trabalho de negociação
1. Espere por uma vela finalizada e armazene seu preço de fechamento.
2. Obtenha o fechamento de `BarsToCheck` velas atrás e calcule a diferença.
3. Se o movimento absoluto estiver abaixo de `MinMovePoints * PriceStep`, não faça nada.
4. Caso contrário:
   * No modo **Seguir**, compre se o preço subir e venda se o preço cair.
   * No modo **Reverso**, compre se o preço cair e venda se o preço subir.
5. Em cada vela finalizada subsequente enquanto a posição estiver aberta:
   * Fechar posições compradas quando o fechamento estiver `TakeProfitPoints` acima ou `StopLossPoints` abaixo do preço de entrada armazenado.
   * Fechar posições vendidas quando o fechamento estiver `TakeProfitPoints` abaixo ou `StopLossPoints` acima da entrada.
   * Forçar o fechamento assim que `BarsToHold` velas tiverem decorrido desde a entrada.

## Notas de uso
* A conversão de pontos em preço absoluto requer `Security.PriceStep`. Forneça os metadados corretos do instrumento (etapa de preço, preço escalonado, regras de volume) antes de executar a estratégia.
* A estratégia não gerencia múltiplas posições simultâneas; certifique-se de que `Volume` corresponda ao tamanho esperado para um único pedido MT4.
* Como as decisões são avaliadas apenas em velas concluídas, a estratégia deve ser executada com dados históricos e em tempo real que forneçam barras concluídas.
