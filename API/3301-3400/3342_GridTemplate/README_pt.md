# Estratégia de modelo de grade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do MetaTrader 4 consultor especialista **Grid_Template**. Ele constrói uma grade simétrica de estoques pendentes
p ordens em torno do lance/pedido atual, permitindo que o trader conecte filtros de entrada personalizados ou execute-os como um modelo de breakout puro. Uma vez
E todas as ordens de rede foram executadas ou expiraram, o mecanismo prepara imediatamente a próxima rede. A implementação preserva t
A fórmula opcional de gerenciamento de dinheiro e a capacidade de remover automaticamente pedidos pendentes obsoletos após um número configurável de
horas.

## Lógica de negociação
- Assine as cotações do Nível 1 para acompanhar continuamente os melhores preços de compra/venda. Não são necessárias velas ou indicadores.
- Sempre que a conta não tiver posição aberta e nenhuma ordem de estratégia ativa, coloque `GridOrders` ordens de stop de compra acima de ask e `G
ordens stop de venda da ridOrders abaixo do lance.
- O primeiro nível da grade é compensado em `PriceDistancePips` do preço de mercado atual; cada nível subsequente adiciona `GridStepPips` m
distância do minério.
- Cada entrada usa o mesmo volume fixo (ou tamanho gerenciado pelo dinheiro) e as mesmas distâncias de stop-loss e take-profit expressas em p
ips.
- Assim que uma ordem pendente é preenchida, a estratégia registra as ordens de proteção correspondentes (stop loss e takeprofit) como
ordens de stop/limit independentes. Eles herdam o mesmo comentário para torná-los fáceis de identificar.
- Se nenhuma ordem for acionada antes que o temporizador de expiração termine, o modelo cancela todas as ordens pendentes restantes e rearma o g
livrar.

## Gestão de dinheiro
- Quando `UseMoneyManagement` está desabilitado, todos os pedidos usam o parâmetro fixo `StaticVolume`.
- Quando ativado, o tamanho do lote é derivado da fórmula do modelo original: `freeMargin * RiskPercent / 100000`, arredondado para n
earer `VolumeStep` e preso entre `VolumeMin` e `VolumeMax`. O valor atual do portfólio é usado como substituto para o MT4
margem livre.
- O volume calculado é normalizado pelas configurações do contrato de câmbio; se cair abaixo do tamanho mínimo negociável, será definido como
zero, impedindo o envio do pedido.

## Gerenciamento de pedidos e riscos
- As ordens stop de compra são colocadas em `ask + PriceDistancePips + GridStepPips * level`. As ordens stop de venda refletem a lógica do bid si
de.
- Paradas de proteção (`SellStop`/`BuyStop`) e metas (`SellLimit`/`BuyLimit`) são registradas somente após o preenchimento de uma entrada pendente
. Isso imita o comportamento do MT4, onde o stop loss e o takeprofit pertencem ao mesmo ticket.
- `PendingExpirationHours` define por quanto tempo as ordens de entrada pendentes permanecem ativas. Um valor zero os mantém até que eles preencham ou sejam ma
cancelado anualmente.
- Quando a posição líquida retorna a zero, a estratégia também cancela quaisquer ordens de proteção ainda ativas para garantir uma ficha limpa.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `OrderComment` | Texto atribuído a cada pedido gerado pela grade, correspondendo ao comentário EA original. |
| `StaticVolume` | Tamanho de lote fixo usado quando o gerenciamento de dinheiro está desativado. |
| `UseMoneyManagement` | Ativa a rotina de dimensionamento baseada em balanceamento. |
| `RiskPercent` | Percentagem utilizada pela fórmula de gestão de dinheiro; ignorado quando `UseMoneyManagement` é falso. |
| `TakeProfitPips` | Distância de lucro aplicada a cada entrada da grade. |
| `StopLossPips` | Distância de stop-loss aplicada a cada entrada da grade. |
| `PriceDistancePips` | Gap inicial (em pips) entre o preço de mercado e a primeira ordem da grade. |
| `GridStepPips` | Distância adicional (em pips) adicionada entre níveis de grade consecutivos. |
| `GridOrders` | Número de ordens pendentes criadas em cada lado do preço. |
| `PendingExpirationHours` | Vida útil da grade pendente antes do cancelamento. |

## Notas
- O modelo não impõe filtros baseados em indicadores; comerciantes podem estender a classe e substituir `TryPlaceGrid` para adicionar custo
m condições.
- Como as paradas e metas de proteção são implementadas como ordens separadas, a execução do lado da corretora pode diferir ligeiramente do MT4 tik
gerenciamento de stop-loss/take-profit estilo t, especialmente em preenchimentos parciais.
- Sempre confirme se o tamanho do pip inferido da troca (`PriceStep` e `Decimals`) corresponde ao instrumento que está sendo negociado.
antes de executar a estratégia em uma conta real.
