# Estratégia Elite eFibo Trader v2.1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Elite eFibo Trader v2.1 recria o consultor especialista MetaTrader que empilha pedidos do tamanho de Fibonacci em uma direção enquanto compartilha um stop de proteção comum. A porta StockSharp mantém o comportamento original: uma única ordem de mercado lança uma sequência de ordens stop espaçadas por `LevelDistancePips`, e cada nível preenchido aumenta a exposição de acordo com a progressão Fibonacci. A estratégia fecha imediatamente toda a cesta assim que o stop compartilhado é tocado ou quando o lucro flutuante atinge `MoneyTakeProfit`.

O algoritmo é intencionalmente direcional. Defina `OpenBuy` como `true` (e `OpenSell` como `false`) para negociar retrocessos de alta ou gire os interruptores para executar a variante de baixa. Apenas uma escada está ativa por vez, espelhando a lógica de ciclo único do script MQL4.

## Requisitos de dados
- Assina o fluxo de negociação para recuperar o preço de execução mais recente usado para colocação em escada, lógica de rastreamento e avaliação de lucro de dinheiro.
- Baseia-se nos metadados de segurança (`PriceStep`, `StepPrice`, `VolumeStep`) para traduzir entradas de pip no estilo MetaTrader em preços de câmbio e tamanhos de lote.

## Construção de escada
1. Quando não há exposição e a negociação é permitida, a estratégia verifica as mudanças de direção. Exatamente um entre `OpenBuy` ou `OpenSell` deve ser verdadeiro; caso contrário, nenhuma escada será iniciada.
2. O primeiro nível Fibonacci é aberto no mercado. Os níveis subsequentes são programados como ordens stop compensadas em `LevelDistancePips * pipSize` do preço de referência registrado quando a escada começa.
3. Os volumes vêm dos parâmetros `Level1Volume`… `Level14Volume` e são normalizados para a segurança `VolumeStep`.
4. Todos os níveis herdam o mesmo deslocamento de parada: `StopLossPips * pipSize`. O preço stop é calculado por preenchimento e posteriormente reduzido para que cada ordem ativa compartilhe o nível de proteção mais próximo.

## Parar o gerenciamento
- Cada ordem preenchida armazena seu preço de entrada e seu stop inicial derivado do deslocamento do pip.
- Em cada tick de negociação, a estratégia reavalia todas as paradas abertas e as alinha com o valor mais apertado em toda a escada (parada mais alta para posições compradas, parada mais baixa para posições vendidas) para imitar as repetidas chamadas `OrderModify` de MetaTrader.
- Quando o último preço de negociação cruza qualquer stop partilhado, a estratégia cancela as restantes ordens pendentes e fecha todo o cabaz com ordens de mercado.

## Gestão de dinheiro
- O lucro não realizado é calculado a partir dos instrumentos `PriceStep` e `StepPrice` para que a meta de caixa espelhe as leituras de `OrderProfit()` de MetaTrader.
- Se o lucro flutuante atingir ou exceder `MoneyTakeProfit`, todas as posições serão fechadas e as ordens pendentes serão canceladas imediatamente.
- Quando `TradeAgainAfterProfit` é `false`, a estratégia permanece ociosa após atingir a meta de dinheiro até que seja reiniciada manualmente.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| `OpenBuy` | Permitir que a estratégia construa uma escada de alta (deve ser exclusiva com `OpenSell`). |
| `OpenSell` | Permitir que a estratégia construa uma escada de baixa (deve ser exclusiva com `OpenBuy`). |
| `TradeAgainAfterProfit` | Retomar a negociação após o fechamento da cesta com o lucro líquido. |
| `LevelDistancePips` | Distância em MetaTrader pips entre ordens de stop consecutivas. |
| `StopLossPips` | Distância em MetaTrader pips usada para derivar a parada de proteção para cada nível preenchido. |
| `MoneyTakeProfit` | Meta de lucro em dinheiro que fecha toda a cesta. |
| `Level1Volume` … `Level14Volume` | Volumes usados para cada nível Fibonacci; definido como zero para pular um nível. |

## Notas de implementação
- A conversão do pip segue a convenção MetaTrader: se o símbolo tiver 3 ou 5 casas decimais, o pip efetivo é igual a `PriceStep * 10`.
- `StartProtection()` é chamado uma vez durante a inicialização para ativar as verificações de segurança StockSharp integradas.
- A lógica de parada compartilhada mantém intencionalmente todos os pedidos sincronizados; uma vez que um stop mais apertado aparece, ele é propagado para todos os níveis ativos.
- Os pedidos pendentes são limpos automaticamente sempre que a escada é plana, replicando as múltiplas chamadas `subCloseAllPending()` encontradas no código MQL.

## Dicas de uso
- Certifique-se de que `PriceStep`, `StepPrice` e `VolumeStep` estejam configurados no instrumento; caso contrário, as conversões de pip ou as metas monetárias podem ser imprecisas.
- Os sistemas de média podem acumular grandes exposições rapidamente. Verifique os limites de volume e os requisitos de margem antes de executar a estratégia ao vivo.
- Desative `TradeAgainAfterProfit` para reproduzir o comportamento único em que EA para de negociar após fechar uma cesta lucrativa.
