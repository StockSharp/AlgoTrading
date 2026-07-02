# Estratégia de Trader Elite eFibo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Elite eFibo Trader reproduz o consultor especialista em média que abre uma progressão Fibonacci de pedidos enquanto monitora um cruzamento de média móvel e um filtro RSI opcional. A porta StockSharp mantém a lógica da cesta original: uma entrada no mercado aciona uma pilha de ordens de stop pendentes espaçadas por distâncias de pip configuráveis, e cada preenchimento adicional aumenta a exposição seguindo a sequência Fibonacci. A estratégia nivela automaticamente a cesta quando o lucro flutuante atinge uma meta de caixa ou quando o filtro de tendência se volta contra a exposição atual.

## Dados de mercado
- Assina um único tipo de vela configurável (padrão: velas de 15 minutos).
- Usa o fechamento da vela para valores do indicador e para avaliar condições de trailing/stop.

## Lógica de entrada
1. A direção é determinada pelo cruzamento da média móvel (ativado por padrão) ou pelas alternâncias manuais `ManualOpenBuy`/`ManualOpenSell`.
2. Quando a lógica MA está ativa, um cruzamento de alta (`fast` acima de `slow`) arma compra cestas e um cruzamento de baixa vende cestas. Um único sinal por vela é aplicado.
3. Se o filtro RSI estiver ativado, cestas longas exigem `RSI > RsiHigh` enquanto cestas curtas exigem `RSI < RsiLow`.
4. Uma nova escada é aberta somente quando não há ordens ou posições ativas da estratégia e a negociação é permitida (`TradeAgainAfterProfit`).
5. O primeiro nível é aberto com uma ordem de mercado, enquanto os níveis restantes são submetidos como ordens stop compensadas por `LevelDistancePips`. Os volumes seguem a sequência Fibonacci e podem ser ajustados nível por nível.

## Lógica de saída
- Cada nível preenchido recebe uma parada inicial calculada a partir de `StopLossPips` e participa de uma atualização final quando a lógica MA detecta um cruzamento adverso.
- As paradas são seguidas até `close - TrailingStopPips` para cestas longas e `close + TrailingStopPips` para cestas curtas, nunca se afastando mais do que a parada atual.
- Quando o preço atinge um nível de stop (com base na máxima/mínima da vela), a estratégia fecha o volume restante desse nível com uma ordem de mercado.
- Se o lucro flutuante da cesta (calculado a partir dos instrumentos `PriceStep` e `StepPrice`) atingir `MoneyTakeProfit`, todas as posições serão fechadas e as ordens pendentes serão canceladas.
- Depois que a cesta estiver estável, quaisquer ordens de stop pendentes serão canceladas automaticamente. Se `TradeAgainAfterProfit` for `false` a estratégia permanece inativa até ser redefinida.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| `UseMaLogic` | Habilite ou desabilite a lógica de cruzamento da média móvel que define a direção da negociação. |
| `MaSlowPeriod`, `MaFastPeriod` | Períodos dos SMAs lentos e rápidos. |
| `TrailingStopPips` | Distância pip usada pelo trailing stop de proteção quando o filtro de tendência fica adverso. |
| `UseRsiFilter`, `RsiPeriod`, `RsiHigh`, `RsiLow` | RSI configuração de filtro. O filtro permite posições compradas acima de `RsiHigh` e vendidas abaixo de `RsiLow`. |
| `ManualOpenBuy`, `ManualOpenSell` | Alternâncias manuais usadas quando a lógica MA está desabilitada. |
| `TradeAgainAfterProfit` | Retome a negociação depois de atingir o lucro líquido. |
| `LevelDistancePips` | Distância em pips entre ordens pendentes consecutivas. |
| `StopLossPips` | Deslocamento de parada inicial para cada nível. |
| `MoneyTakeProfit` | Meta de lucro monetário avaliada no PnL aberto da cesta. |
| `Level1Volume` … `Level14Volume` | Volume de cada nível Fibonacci. Defina como zero para desativar um nível. |
| `CandleType` | Prazo/tipo de dados usado para indicadores. |

## Notas de implementação
- As distâncias pip são convertidas de pontos estilo MetaTrader multiplicando o instrumento `PriceStep` por dez quando o título tem 3 ou 5 casas decimais. Isso reflete o ajuste original `MyPoint` para cotações de câmbio de 5 dígitos.
- Cada nível é rastreado de forma independente. A estratégia armazena o preço de entrada, o volume restante e o nível de stop para que os preenchimentos parciais e os stop-outs individuais sejam tratados da mesma forma que o especialista MQL.
- O lucro flutuante é calculado a partir de `PriceStep` e `StepPrice`. Certifique-se de que as propriedades do instrumento estejam configuradas, caso contrário, o lucro do dinheiro não será acionado corretamente.
- `StartProtection()` é invocado uma vez durante a inicialização para ativar as verificações de segurança integradas da classe base da estratégia StockSharp.
- Quando nenhum volume aberto permanece, `CancelAllPendingOrders()` é chamado automaticamente, replicando as chamadas `subCloseAllPending()` repetidas do script original.

## Dicas de uso
- Verifique as configurações do corretor para `PriceStep`, `StepPrice`, `VolumeStep` e tamanho mínimo do lote para garantir que os volumes de Fibonacci sejam convertidos em pedidos válidos.
- A estratégia depende de dados de velas; certifique-se de que o período selecionado corresponda ao período do gráfico MetaTrader pretendido.
- Considere executar a estratégia primeiro em feeds de demonstração: os sistemas de média podem acumular grande exposição durante tendências adversas.
- Desative `UseMaLogic` para reproduzir a polarização manual usada nas entradas originais EA ou mantenha-o ativado para detecção automática de tendências.
