# Estratégia do sistema de anel EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia transfere o especialista em hedge de grade multimoeda "RingSystemEA" de MetaTrader 4 para o StockSharp alto nível API. Ele organiza uma lista configurável de moedas em anéis triangulares (três moedas geram três pares correlacionados) e gerencia duas cestas cobertas por anel: uma cesta **mais** (longa/curta/longa) e uma cesta **menos** (curta/longa/curta). A estratégia monitora continuamente o lucro flutuante em cada anel, aplica reforço estilo martingale baseado em etapas quando as perdas excedem os limites configurados e coordena saídas globais ou por lado quando as metas de lucro ou perda são atingidas.

## Lógica de negociação

* Crie todas as combinações exclusivas de três moedas da lista ordenada `CurrenciesTrade` (por exemplo, EUR/GBP/AUD produz EURGBP, EURAUD e GBPAUD).
* Cada anel mantém duas cestas sincronizadas:
  * **Cesta Plus** abre COMPRA no primeiro par, VENDA no segundo par, COMPRA no terceiro par.
  * **Cesta negativa** abre a sequência espelhada de VENDA/COMPRA/VENDA.
* As cestas são abertas automaticamente assim que o anel possui dados de preço e o filtro da sessão permite a negociação. Ambos os lados podem funcionar simultaneamente ou apenas um lado dependendo de `SideOpenOrders`.
* Quando uma cesta ativa ultrapassa o limite `StepOpenNextOrders` (opcionalmente dimensionado geométrica ou exponencialmente), uma nova camada de pedidos é adicionada usando regras de progressão de volume (`LotOrdersProgress`).
* As cestas são fechadas quando seu PnL flutuante satisfaz o modo de saída escolhido:
  * `SingleTicket` fecha as cestas de mais e menos de forma independente.
  * `BasketTicket` fecha as duas cestas quando o lucro combinado atinge a meta.
  * `PairByPair` fecha pares individuais quando seu PnL excede a meta.
* As saídas de proteção espelham a lógica MT4. Dependendo de `TypeCloseInLoss`, a estratégia fecha cestas inteiras, reduz a exposição pela metade ou permite que as cestas se recuperem sem saídas forçadas.
* O protetor de sessão opcional replica o comportamento de esperar depois de abrir na segunda-feira e parar antes de fechar na sexta.
* Os parâmetros correspondem aproximadamente ao EA original. O dimensionamento automático do lote usa o valor atual do portfólio e `RiskFactor`, enquanto a opção "lote justo" compensa as diferenças de valor do tick dentro de um anel.

## Parâmetros principais

| Parâmetro | Descrição |
| --- | --- |
| `CurrenciesTrade` | Lista ordenada de moedas que define como os anéis são gerados. |
| `NoOfGroupToSkip` | Números de toque separados por vírgula a serem ignorados. |
| `SideOpenOrders` | Escolha o lado positivo, o lado negativo ou ambos. |
| `OpenOrdersInLoss` + `StepOpenNextOrders` | Controla quando pedidos adicionais são adicionados enquanto as cestas estão perdendo. |
| `StepOrdersProgress` | Multiplicador aplicado ao limite de perda para cada camada adicional. |
| `LotOrdersProgress` | Regra de escalonamento para volumes de pedidos subsequentes. |
| `TypeCloseInProfit` / `TargetCloseProfit` | Lógica e limites de obtenção de lucro. |
| `TypeCloseInLoss` / `TargetCloseLoss` | Saídas protetoras em perda. |
| `AutoLotSize`, `RiskFactor`, `ManualLotSize`, `UseFairLotSize` | Opções de gerenciamento de dinheiro. |
| `ControlSession`, `WaitAfterOpen`, `StopBeforeClose` | Proteção de janela de negociação semanal. |
| `MaxSpread`, `MaximumOrders`, `MaxSlippage` | Restrições de risco. |

## Notas Comportamentais

* A porta StockSharp mantém o estado em estruturas gerenciadas em vez de matrizes brutas, mas o fluxo de negociação reflete o especialista MT4: abre cestas balanceadas, monitora o PnL da cesta, reforça nas etapas de rebaixamento e fecha em eventos de lucro ou risco.
* Todos os indicadores estão implícitos; a estratégia depende exclusivamente de assinaturas de preços e PnL da conta para tomar decisões.
* Os pedidos são marcados com `StringOrdersEA` para que ferramentas externas de pós-processamento possam identificá-los.
* As ordens de mercado utilizam o portfólio de estratégia; conecte os instrumentos desejados antes de começar.

## Diferenças do original EA

* A filtragem de propagação é simplificada: a porta StockSharp valida o `MaxSpread` configurado por meio de dados de velas em vez de instantâneos de ticks.
* O modo de etapa automática reutiliza o valor da etapa manual porque os cálculos de margem específicos de MetaTrader não estão disponíveis em StockSharp.
* Os recursos de desenho da interface do usuário e registro de arquivos da versão MT4 são omitidos. `SaveInformations` agora grava diagnósticos detalhados no log em vez de no gráfico.
* O dimensionamento da posição utiliza o valor atual do portfólio; ajuste `RiskFactor` para calibrar o volume.

## Dicas de uso

1. Conecte e mapeie todos os pares de moedas referenciados por `CurrenciesTrade`. Os auxiliares de prefixo/sufixo suportam símbolos específicos do corretor.
2. Defina `SideOpenOrders` para controlar se a estratégia deve manter ambas as cestas ou operar em uma única direção.
3. Ajuste `StepOpenNextOrders`, `StepOrdersProgress` e `LotOrdersProgress` com cuidado; esses parâmetros moldam a progressão do martingale e a exposição ao risco.
4. Revise as mensagens de registro quando `SaveInformations` estiver ativado para entender como os anéis evoluem e quando as cestas são adicionadas ou fechadas.

Esta porta preserva o comportamento central da grade protegida do especialista MT4 enquanto o adapta à arquitetura orientada a eventos e ao sistema de parâmetros de StockSharp.
