# Mais pedidos após o ponto de equilíbrio (StockSharp Porto)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta pasta contém uma porta StockSharp C# do consultor especialista MetaTrader 4 **"More Orders After BreakEven"** (MQL ID de origem `35609`). O EA original adiciona repetidamente novas posições longas, uma vez que as negociações anteriores tenham sido protegidas no ponto de equilíbrio. A porta reproduz esse gerenciamento de dinheiro baseado em bilhetes enquanto se integra ao StockSharp de alto nível do API.

## Visão geral da estratégia

* **Lado do mercado** – apenas comprado. Cada negociação é uma ordem de compra de mercado colocada no título primário da estratégia.
* **Ideia central** – embora haja menos negociações abertas sem proteção de ponto de equilíbrio do que `MaximumOrders`, a estratégia compra novamente. Quando uma negociação existente atinge a distância do ponto de equilíbrio, o seu stop-loss é aumentado para o preço de entrada, para que não bloqueie mais entradas adicionais.
* **Gerenciamento de saídas** – cada pedido armazena seus próprios níveis de stop-loss e take-profit. Os stops são movidos para o ponto de equilíbrio quando o preço avança `BreakEvenPips`. As ordens de venda no mercado fecham posições quando o preço de compra atinge qualquer um dos níveis de proteção.
* **Processamento de ticks** – o EA original funcionou em cada tick via `OnTick`. A porta usa dados de mercado de nível 1 para monitorar os melhores preços de compra/venda e emula o mesmo comportamento: cada atualização avalia entradas, regras de equilíbrio e saídas potenciais.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-------------|---------|
| `MaximumOrders` | Número máximo de negociações longas cujo stop loss ainda não atingiu o ponto de equilíbrio. Assim que a contagem cair abaixo deste limite, novas posições poderão ser abertas. | `1` |
| `TakeProfitPips` | Distância do preço de entrada até a meta de lucro expressa em MetaTrader pips. Um valor de `0` desativa o take-profit. | `100` |
| `StopLossPips` | Distância inicial até a parada de proteção em MetaTrader pips. Defina como `0` para sair da posição sem um stop inicial (a regra do ponto de equilíbrio ainda pode protegê-la mais tarde). | `200` |
| `BreakEvenPips` | Distância de lucro (em MetaTrader pips) após a qual o stop-loss é elevado para o preço de entrada. `0` significa que o stop se move para o ponto de equilíbrio assim que o preço excede o preço de entrada. | `10` |
| `TradeVolume` | Volume enviado com cada ordem de compra de mercado. | `0.01` |
| `DebugMode` | Quando ativada, a estratégia registra mensagens informativas que imitam a saída `Comment()` do EA original. | `true` |

Todas as distâncias baseadas em pip se adaptam automaticamente aos símbolos Forex de 4/2 e 5/3 dígitos, analisando o tamanho do tick e a precisão decimal do instrumento, replicando o fator de escala `points` do código original.

## Lógica de negociação

1. **Assinatura de nível 1** – a estratégia assina as melhores cotações de compra/venda. Cada vez que ambos os preços são conhecidos, `ProcessPrices` emula o loop MQL `OnTick`.
2. **Contagem de pedidos** – antes de fazer um novo pedido, a estratégia conta as entradas abertas que ainda não atingiram o ponto de equilíbrio. Isso reproduz o auxiliar `OrdersCounter()` original.
3. **Entradas** – quando a contagem está abaixo de `MaximumOrders`, uma nova ordem de compra ao mercado é enviada usando `TradeVolume`. O preço de preenchimento é registrado e os níveis de stop/take-profit por ticket são inicializados.
4. **Atualização do ponto de equilíbrio** – para cada entrada ativa, o preço do lance é comparado com o acionador do ponto de equilíbrio. Uma vez excedido, o stop-loss é movido para o preço de entrada, marcando o ticket como protegido para que não contribua mais para a contagem da ordem.
5. **Verificações de saída** – o preço do lance também impulsiona a detecção de saída. Se atingir o take-profit armazenado ou cair para o stop loss (incluindo o stop de equilíbrio), a estratégia emite uma ordem de venda a mercado para o volume restante desse bilhete.
6. **Rastreamento de posição** – preenchimentos recebidos por meio de `OnOwnTradeReceived` mantêm uma lista FIFO de entradas. Isso reproduz o comportamento do ticket de MetaTrader, onde cada pedido pode ser tratado individualmente, mesmo que StockSharp agregue a posição líquida.

## Diferenças do original EA

* Apenas negociações longas são implementadas porque a versão MQL nunca emitiu entradas de venda.
* As ordens stop e take-profit do lado do corretor são substituídas por monitoramento do lado estratégico e saídas de mercado. Isso é necessário porque StockSharp não modifica automaticamente as paradas por pedido no API de alto nível.
* A saída de diagnóstico usa o sistema de registro de StockSharp em vez do texto `Comment()` no gráfico MetaTrader.

## Notas de uso

1. Anexe a estratégia a um conector que forneça dados de nível 1 para a segurança escolhida.
2. Configure os parâmetros baseados em pip para corresponder à volatilidade do instrumento e aos requisitos do corretor.
3. Ative `DebugMode` durante o teste para verificar a contagem de pedidos e o comportamento do ponto de equilíbrio e, em seguida, desative-o na produção para obter registros mais silenciosos.
4. Como as saídas são tratadas por meio de ordens de mercado, certifique-se de que a carteira tenha poder de compra disponível suficiente para cobrir todas as entradas adicionais que possam ser acionadas quando a proteção do ponto de equilíbrio entrar em vigor.

## Referência de fonte

* Arquivo MQL4 original: `MQL/35609/More Orders After BreakEven.mq4`.
* Estratégia C# convertida: `CS/MoreOrdersAfterBreakEvenStrategy.cs`.
