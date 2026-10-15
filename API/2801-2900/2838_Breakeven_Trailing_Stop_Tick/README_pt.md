# Estratégia de Trailing Stop com Ponto de Equilíbrio por Ticks
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Gerenciador de trailing stop baseado em ticks convertido do consultor especialista MetaTrader `e_Breakeven_v4`.
- Monitora cada tick de negociação para mover um stop-loss virtual quando o preço se afasta o suficiente da entrada.
- Fecha posições compradas ou vendidas a mercado quando o nível de trailing é atingido, replicando o comportamento de ponto de equilíbrio mais passo do EA original.
- Inclui um modo demo opcional que abre posições aleatórias durante os testes para demonstrar a lógica de trailing sem uma fonte de sinal externa.

## Como funciona
1. A estratégia assina ticks de negociação (`DataType.Ticks`) para emular o callback `OnTick` usado no MQL5.
2. Quando uma posição existe e o trailing stop (em pips) mais o passo de trailing foram excedidos, o nível de stop é deslocado mais perto do preço.
3. Para posições compradas, o stop é colocado em `preço atual - trailing stop` se o movimento desde a entrada exceder `trailing stop + trailing step`.
4. Para posições vendidas, o stop é colocado em `preço atual + trailing stop` quando o preço se move para baixo a mesma distância.
5. Se o preço ao vivo tocar ou cruzar o nível de stop armazenado, a estratégia sai de toda a posição a mercado e reinicia o estado de trailing.
6. Uma conversão interna de pips multiplica o passo de preço do broker por 10 quando o instrumento tem 3 ou 5 casas decimais, correspondendo ao ajuste ponto-a-pip do MQL5.
7. Quando o modo demo está habilitado, a estratégia abre uma negociação comprada ou vendida aleatoriamente (usando o `Volume` configurado) na primeira vez que um novo tick chega após a entrada anterior ser fechada.

## Parâmetros
| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `TrailingStopPips` | Distância em pips entre o preço atual e o trailing stop. | 10 | Definir como `0` para desabilitar o trailing completamente. |
| `TrailingStepPips` | Distância adicional em pips necessária antes que o stop avance novamente. | 1 | Deve ser maior que zero quando o trailing stop está ativo, reproduzindo a regra de validação do EA. |
| `EnableDemoEntries` | Habilita entradas aleatórias para backtests sem um sinal externo. | `false` | Quando `true`, a estratégia lança uma moeda em cada tick enquanto está zerada para decidir a direção. |

## Regras de gerenciamento de posição
- A estratégia não abre posições por si mesma a menos que `EnableDemoEntries` esteja em `true`.
- O trailing é simétrico para posições compradas e vendidas e funciona com qualquer tamanho de volume.
- Os níveis de stop são gerenciados internamente (virtuais) e aplicados com saídas a mercado, evitando ordens stop explícitas que podem não ser suportadas por todos os conectores.
- Qualquer negociação manual ou estratégia externa pode fornecer as entradas; este componente só gerenciará o trailing stop.

## Notas de uso
- Funciona melhor com instrumentos que fornecem ticks de negociação para que o trailing reaja imediatamente.
- Certifique-se de que `Volume` esteja configurado para o tamanho de lote que corresponde às posições recebidas se o modo demo for usado.
- A conversão de pips assume preços no estilo FX onde símbolos com 3 ou 5 decimais precisam de um multiplicador ×10 para converter pontos em pips.
- A saída é acionada no primeiro tick que cruza o preço de stop armazenado, correspondendo ao fluxo imediato de modificação e fechamento da lógica MQL.

## Diferenças em relação ao especialista MQL5 original
- Usa stops virtuais com saídas a mercado em vez de modificar ordens stop-loss do lado do broker porque as estratégias StockSharp tipicamente gerenciam saídas através da lógica da estratégia.
- Substitui o bloco de entrada aleatória do testador MetaTrader pelo flag configurável `EnableDemoEntries`.
- Converte a lógica ponto-a-pip usando `Security.PriceStep` e contagem de decimais em vez de `Symbol().Digits()`.
- Todos os comentários e registros agora estão em inglês de acordo com as diretrizes do repositório.
