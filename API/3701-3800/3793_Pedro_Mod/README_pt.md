# Estratégia do Mod Pedro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma versão StockSharp do consultor especialista **Pedroxxmod** MetaTrader 4. O EA original espera que o mercado mova um
alguns pips de distância de um preço de referência e então abre uma posição contrária. Os pedidos subsequentes são calculados na mesma direção
sempre que o preço retrocede por uma distância configurável. A implementação StockSharp mantém o comportamento intacto ao expor
parâmetros fortemente digitados por meio do `Strategy` API de alto nível.

## Lógica de negociação

1. Assine as melhores cotações de compra/venda da Level1 e armazene em cache os valores mais recentes.
2. Quando nenhuma negociação estiver aberta, armazene o preço de venda atual como nível de entrada de referência. A negociação só é permitida entre
`StartHour` e `EndHour`, e de `StartYear` em diante.
3. Se a melhor venda subir `Gap` MetaTrader pips acima da referência, envie uma ordem de venda a mercado. Se cair `Gap` pips,
enviar uma ordem de compra de mercado. Os níveis protetores de stop-loss e take-profit são anexados automaticamente chamando
`SetStopLoss` / `SetTakeProfit` com as mesmas distâncias de pip que o consultor especialista.
4. Uma vez estabelecida uma direção de cesta, a estratégia mantém uma lista FIFO das posições sintéticas para emular o hedge.
estilo de MetaTrader. Contanto que o tamanho atual da cesta seja inferior a `MaxTrades`, a média dos pedidos será adicionada quando a melhor oferta
retorna dentro de `ReEntryGap` pips do último preço de entrada.
5. O gerenciamento de dinheiro pode usar o parâmetro fixo `Lots` ou alocar volume dinamicamente de acordo com a regra EA
`floor(Equity / 20000)`, limitado por `MaxLots`. Todos os volumes são normalizados em relação à etapa/mín/máx do volume da segurança.
6. As atualizações fora do horário comercial redefinem as âncoras de entrada internas para evitar negociações falsas quando a próxima sessão começar.

## Parâmetros

| Nome | Descrição |
|------|-------------|
| `Lots` | Volume de pedidos fixo quando o gerenciamento de dinheiro está desativado. |
| `StopLoss` | Distância de parada protetora em MetaTrader pips. Defina como `0` para desativar a parada. |
| `TakeProfit` | Distância alvo de lucro em MetaTrader pips. Defina como `0` para desativar o alvo. |
| `Gap` | Distância em MetaTrader pips que o pedido deve se afastar da referência antes de abrir a primeira negociação. |
| `MaxTrades` | Número máximo de negociações abertas simultaneamente (tamanho da cesta). |
| `ReEntryGap` | Distância em MetaTrader pips que aciona a média dos pedidos na direção da cesta. |
| `MoneyManagement` | Ativa a regra de volume dinâmico `floor(Equity / 20000)` quando definida como `true`. |
| `MaxLots` | Limite superior para o volume calculado dinamicamente. |
| `StartHour` / `EndHour` | Janela de negociação no horário do servidor Exchange (inclusive). |
| `StartYear` | Ano civil a partir do qual a negociação é permitida. Os dados anteriores são ignorados. |

## Notas

- A estratégia consome apenas dados do Nível 1 e não solicita velas. É, portanto, leve e reage imediatamente a
alterações de cotação, assim como o manipulador de ticks MT4 `start()`.
- Paradas e alvos dependem dos métodos auxiliares de `Strategy` para traduzir distâncias de MetaTrader pip em específicas do corretor
níveis de preços. Certifique-se de que o local conectado exponha os valores `PriceStep`, `StepPrice` e `VolumeStep` corretos.
- O contador de cesta sintético permite que a estratégia imite contas de hedge, mesmo que StockSharp agregue a posição.
Preenchimentos parciais e ocorrências de parada são tratados por meio do retorno de chamada `OnPositionChanged` que mantém as filas FIFO.
- A implementação do Python é omitida intencionalmente de acordo com as diretrizes do repositório.
