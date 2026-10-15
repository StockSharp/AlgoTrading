# Exemplo de estratégia de verificação de pedido pendente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Sample Check Pending Order garante continuamente que exatamente uma ordem de compra e uma ordem de venda estejam no livro. O especialista MetaTrader 5 original da Tungman verifica se o corretor aceita o tamanho do lote solicitado, confirma que há margem livre suficiente para ambas as direções e, em seguida, envia novos pedidos pendentes logo acima do lance/venda atual com vencimento em um dia. Esta conversão reproduz o mesmo fluxo de trabalho usando o gerenciamento de pedidos de alto nível API e cotações de nível 1 do StockSharp.

## Lógica de negociação

1. **Processamento de dados de mercado**
   - A estratégia assina atualizações de Nível 1 e armazena em cache os melhores preços de compra e venda mais recentes.
   - A lógica de negociação é suspensa até que ambos os lados do livro sejam conhecidos e `IsFormedAndOnlineAndAllowTrading()` confirme que o ambiente está pronto (a estratégia está em execução, o portfólio está conectado, etc.).
2. **Volume validation**
   - Cada tick recebido aciona uma validação do `OrderVolume` configurado em relação a `Security.MinVolume`, `Security.MaxVolume` e `Security.VolumeStep`.
   - A verificação reflete o auxiliar MT5: o volume deve estar dentro da faixa permitida e ser um múltiplo exato da etapa. As violações produzem uma entrada de registro informativa e bloqueiam quaisquer novos pedidos.
3. **Pré-verificação de margem**
   - Antes de enviar qualquer coisa, a estratégia estima a margem necessária para colocar uma posição comprada ou vendida do tamanho configurado. Ele usa o último lance/venda, o multiplicador do instrumento e o `AccountLeverage` fornecido pelo usuário para calcular o requisito.
   - Se o valor atual ou inicial do portfólio for insuficiente para qualquer direção, o algoritmo aborta para esse tick, imitando de perto as salvaguardas `CheckMoneyForTrade`.
4. **Colocação de pedido pendente**
   - Quando não existe nenhuma ordem buy-stop ativa, uma nova é registrada na oferta atual (arredondada para o tick mais próximo). A mesma regra se aplica ao sell-stop na oferta atual. Ambos os pedidos reutilizam o mesmo resultado de validação de volume.
   - A expiração é imposta manualmente: cada pedido armazena seu limite de tempo (`ExpirationMinutes`, um dia por padrão). Os ticks futuros cancelam a ordem se o prazo tiver passado e liberam imediatamente o slot para uma nova ordem pendente.
5. **Gerenciamento de riscos**
   - `StartProtection` estabelece um stop-loss e take-profit absolutos com base em `StopLossPoints` e `TakeProfitPoints`. Assim que um pedido é acionado, StockSharp envia automaticamente as saídas de proteção nas distâncias configuradas, recriando os parâmetros SL/TP usados ​​na versão MT5.

O resultado final é um mecanismo de breakout minimalista que sempre mantém o mercado “encaixotado” entre duas ordens de stop. Sempre que uma ordem é atendida, o outro lado permanece ativo enquanto a estratégia se prepara para reemitir a perna faltante na próxima atualização da cotação.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `OrderVolume` | Tamanho do lote enviado com cada ordem de parada. Deve respeitar os limites da corretora e o passo de volume. |
| `StopLossPoints` | Distância em pontos convertida em unidades de preço para o stop de proteção quando uma negociação é aberta. |
| `TakeProfitPoints` | Distância em pontos utilizada para a meta de lucro criada após um preenchimento. |
| `ExpirationMinutes` | Vida útil de cada pedido pendente. Quando o período expirar, o pedido será cancelado e recriado no próximo tick. |
| `AccountLeverage` | Alavancagem estimada da conta usada para aproximar os requisitos de margem antes de cada envio. |

Todas as distâncias são transformadas em compensações de preços reais usando `Security.PriceStep`. Se o instrumento não expor uma etapa de preço ou multiplicador válido, a estratégia volta para um valor de `1` para manter os cálculos definidos. As mensagens de registro documentam qualquer configuração anormal para que os operadores possam ajustar os parâmetros rapidamente.

## Notas de implementação

- **Ciclo de vida do pedido** – A estratégia rastreia os últimos `Order` objetos retornados por `BuyStop` e `SellStop`. Os métodos auxiliares descartam as referências quando o pedido faz a transição para `Done`, `Canceled` ou `Failed`, garantindo que os pedidos obsoletos não sejam confundidos com os ativos.
- **Tratamento de expiração** – StockSharp exchanges não suportam universalmente a expiração do lado do servidor para ordens de parada. Em vez de depender de campos específicos da corretora, a estratégia monitora os carimbos de data/hora localmente e chama `CancelOrder` quando uma ordem pendente ultrapassa seu prazo.
- **Aproximação de margem** – A disponibilidade de margem é estimada usando o patrimônio do portfólio e a alavancagem configurada. Isso mantém o comportamento próximo de `OrderCalcMargin` sem exigir implementações específicas de troca.
- **Uso de API de alto nível** – Todas as operações dependem dos auxiliares de alto nível `SubscribeLevel1`, `BuyStop`, `SellStop` e `StartProtection`, que correspondem às diretrizes de conversão e mantêm o código conciso.

Esta documentação contém intencionalmente detalhes extensos para que os traders possam compreender todas as nuances da conversão e adaptar com segurança os parâmetros ao ambiente de sua corretora.
