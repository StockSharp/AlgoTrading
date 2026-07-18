# Estratégia de comando de gerenciamento de pedidos ARD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral da estratégia
A estratégia **ARD Order Management** transporta o MetaTrader 4 consultor especialista `ARD_ORDER_MANAGEMENT_.mq4` para a estrutura estratégica de alto nível de StockSharp. O script original expunha um conjunto de comandos manuais – comprar, vender, fechar e modificar – que poderiam ser acionados a partir de scripts externos ou botões da interface do usuário. Cada comando recalculou o volume de negociação a partir da margem livre disponível, abriu ou reverteu posições de mercado e anexou níveis protetores de stop-loss e take-profit em distâncias de pontos fixos.

A versão StockSharp mantém o mesmo modelo de interação. Você orienta o comportamento por meio do parâmetro `Command`; uma vez definido um valor diferente de `None`, a estratégia executa a ação solicitada na próxima atualização de nível 1 e redefine automaticamente o comando de volta para `None`. As ordens de proteção são recriadas a cada nova entrada ou solicitação de modificação para que o stop-loss e o take-profit sempre reflitam os valores atuais dos parâmetros.

## Ciclo de vida do comando
1. **Envio de comando** – quando `Command` é definido como `Buy` ou `Sell`, a estratégia armazena a solicitação e chama imediatamente `ClosePosition()` para nivelar qualquer exposição aberta. As ordens de proteção ativas são canceladas antes que a nova negociação seja considerada, espelhando o ciclo MQL que fechou todos os tickets via `OrderClose`.
2. **Cálculo de volume** – o volume é recalculado para cada comando. Ele usa `Portfolio.CurrentValue` (substituição para `Portfolio.BeginValue`) dividido por `LotSizeDivisor` e dimensionado por `1/1000`, exatamente como `AccountFreeMargin()/lotsize/1000` foi usado em MetaTrader. O resultado é arredondado para `LotDecimals` e limitado por `MinimumVolume`.
3. **Aguardando uma posição plana** – se uma posição estava aberta quando o comando chegou, a nova entrada é adiada até que `Position` caia para zero. A estratégia verifica essa condição em cada tick de nível 1 para evitar acelerar o pipeline de execução assíncrona.
4. **Execução de mercado** – uma vez estável, a estratégia envia `BuyMarket` ou `SellMarket`. Os últimos melhores preços de compra/venda conhecidos são armazenados para que as ordens de proteção sejam derivadas de preços de execução realistas.
5. **Colocação de proteção** – os níveis de stop-loss e take-profit são materializados como ordens de stop e limite separadas. Para negociações longas, o stop fica em `bid − StopLossPoints * PriceStep` e o alvo em `ask + TakeProfitPoints * PriceStep`. As negociações curtas invertem esses cálculos. Os comandos de modificação reutilizam a mesma rotina, mas com `ModifyStopLossPoints` e `ModifyTakeProfitPoints`.
6. **Comando Fechar** – definir `Command` como `Close` cancela todas as ordens de proteção e chamadas `ClosePosition()`. Se a estratégia já estiver plana, o comando simplesmente registra o fato e não faz mais nada.

## Gestão de dinheiro
- **Volume orientado pela margem** – o código inspeciona o valor atual do portfólio para que o volume diminua ou aumente com o capital disponível. Se o parâmetro do divisor cair acidentalmente para zero, o algoritmo volta ao `MinimumVolume` configurado e emite um aviso.
- **Arredondamento** – `LotDecimals` define quantas casas decimais são retidas após o arredondamento. A implementação usa `Math.Round` com `MidpointRounding.AwayFromZero` para que os ajustes positivos e negativos se comportem como MetaTrader de `NormalizeDouble`.
- **Lote mínimo** – após o arredondamento, o tamanho é fixado com `MinimumVolume`, reproduzindo o comportamento original onde valores abaixo de `lotmax` foram promovidos para `0.1`.

## Tratamento de stop-loss e take-profit
- As ordens de proteção são sempre recriadas do zero. As ordens stop ou take existentes são canceladas antes que novas sejam enviadas.
- A estratégia verifica `Security.PriceStep` antes de calcular os preços absolutos. Se a etapa estiver ausente ou não for positiva, as ordens de proteção serão ignoradas e um aviso será registrado.
- Os comandos de modificação (`Command = Modify`) reconstroem a proteção usando as distâncias de modificação dedicadas sem alterar o tamanho da posição atual.

## Requisitos de dados e execução
- Assina dados de Nível 1 via `SubscribeLevel1()` para espelhar as atualizações de cotação de MetaTrader (`Bid`/`Ask`).
- Não necessita de velas ou indicadores; toda a lógica é executada em atualizações de ticks/cotações.
- Usa auxiliares de alto nível (`BuyMarket`, `SellMarket`, `BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`, `CancelOrder`) para permanecer dentro da camada StockSharp recomendada API.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `SlippageSteps` | interno | 4 | Derrapagem permitida expressa em etapas de preço. Armazenado para compatibilidade; StockSharp ordens de mercado são executadas imediatamente e não consomem este valor. |
| `LotDecimals` | interno | 1 | Número de casas decimais retidas após arredondamento do volume calculado. |
| `StopLossPoints` | decimal | 50 | Distância (em pontos de preço) desde a entrada até o stop loss inicial. |
| `TakeProfitPoints` | decimal | 100 | Distância (em faixas de preço) desde a entrada até o take-profit inicial. |
| `LotSizeDivisor` | decimal | 5 | Divide o valor do portfólio antes de escalar para lotes (`freeMargin / divisor / 1000`). |
| `ModifyStopLossPoints` | decimal | 20 | Distância de stop-loss aplicada quando `Command = Modify`. |
| `ModifyTakeProfitPoints` | decimal | 100 | Distância de lucro aplicada quando `Command = Modify`. |
| `MinimumVolume` | decimal | 0,1 | Limite inferior para o volume final após arredondamento. |
| `OrderComment` | corda | `"Placing Order"` | Comentário inserido em cada pedido para facilitar a auditoria. |
| `Command` | `ArdOrderCommand` | `None` | Comando manual para executar. Redefinir automaticamente para `None` depois de processado. |

## Notas de uso
- Defina `Command` por meio da IU ou programaticamente. O comando é processado apenas uma vez por alteração; para repetir uma ação, defina-a novamente para `None` e depois para o valor desejado novamente.
- Como o stop-loss e o take-profit são colocados como ordens independentes, os corretores/bolsas devem oferecer suporte a ordens nativas de stop/limit para o mesmo título. Caso contrário, considere substituí-los por saídas sintéticas no código.
- Slippage é mantido como parâmetro para paridade de documentação com a versão MT4. Os ajudantes de mercado de alto nível de StockSharp não expõem um parâmetro de derrapagem explícito, portanto o valor é apenas informativo.
- A estratégia registra todas as ações importantes (`LogInfo`/`LogWarn`) para auxiliar nas trilhas de auditoria durante a execução discricionária.

## Diferenças em comparação com o consultor especialista MQL original
- MetaTrader anexou paradas e alvos diretamente ao ticket do mercado. Em vez disso, StockSharp emite ordens stop e limit separadas.
- A porta usa o modelo de evento assíncrono de StockSharp. Ao reverter uma posição, a entrada espera até que a posição anterior seja informada como fechada, evitando a sobreposição de ordens.
- As informações do portfólio substituem `AccountFreeMargin`. Certifique-se de que o adaptador de portfólio preencha `CurrentValue` ou configure `BeginValue` antes de iniciar a estratégia.
- O tratamento de erros depende do registro de StockSharp em vez de tentativas repetidas de `OrderSend` porque as exceções de envio de pedidos são apresentadas pela própria estrutura.

## Dicas de teste
- Execute a estratégia em simulação com dados de Nível 1 para confirmar que as ordens de proteção aparecem nas distâncias esperadas.
- Experimente diferentes valores de `LotSizeDivisor` e `LotDecimals` para corresponder às especificações do contrato do corretor antes de usar a estratégia em ambientes ativos.
