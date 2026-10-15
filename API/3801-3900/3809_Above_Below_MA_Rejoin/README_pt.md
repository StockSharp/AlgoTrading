# Acima Abaixo MA Estratégia de Reingresso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Above Below MA Rejoin é uma conversão StockSharp do MetaTrader 4 consultor especialista "AboveBelowMA". O script original monitora o gráfico de 15 minutos do GBP/USD e compara o preço atual com uma média móvel exponencial de um período (EMA) calculada sobre o preço típico. Quando o preço é negociado no lado oposto de uma média ascendente ou descendente, a estratégia tenta atenuar essa excursão e retornar à direção subjacente do EMA. Esta porta mantém a estrutura do sinal intacta enquanto aproveita StockSharp APIs de alto nível (`SubscribeCandles` + `Bind`).

## Lógica de negociação
- Assine o tipo de vela configurado (15 minutos por padrão) e alimente uma média móvel exponencial que usa o preço típico `(High + Low + Close) / 3`.
- Acompanhe os valores EMA mais recentes e anteriores para entender a inclinação de curto prazo. Uma tendência de alta exige que o EMA suba, enquanto uma tendência de baixa exige que ele caia.
- **Configuração longa:** quando a vela abre pelo menos uma etapa de preço abaixo de EMA, fecha abaixo de EMA e o valor anterior de EMA é inferior ao valor atual de EMA, feche qualquer exposição curta e prepare-se para comprar. Se nenhuma posição permanecer, envie uma ordem de compra a mercado.
- **Configuração curta:** quando a vela abre pelo menos uma etapa de preço acima de EMA, fecha acima de EMA e o valor anterior de EMA é superior ao valor atual de EMA, feche qualquer exposição longa e prepare-se para vender. Se a posição for plana, envie uma ordem de venda a mercado.
- As ordens são emitidas apenas para velas finalizadas para evitar sinais prematuros em barras parcialmente formadas.

## Dimensionamento de posição
- A versão MetaTrader dimensiona as negociações usando `AccountFreeMargin / 10000` limitadas a 5 lotes. A implementação StockSharp oferece um comportamento equivalente: quando `UseDynamicVolume` está habilitado, a estratégia divide o valor do portfólio atual por `BalanceToVolumeDivider` (padrão `10000`).
- O tamanho calculado é limitado por `MaxVolume`, refletindo o limite máximo de 5 lotes do consultor especialista. Se o dimensionamento dinâmico estiver desativado, o parâmetro `InitialVolume` será usado como um volume fixo.
- Todos os volumes estão alinhados ao passo de volume do instrumento e às restrições de volume mínimo/máximo para evitar rejeição pelo corretor ou simulador.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `EmaLength` | Período da média móvel exponencial (o padrão é 1, correspondendo a EA). |
| `CandleType` | Período usado para construir as velas que alimentam o EMA (padrão 15 minutos). |
| `InitialVolume` | Volume de pedido fixo quando o dimensionamento dinâmico está desativado. |
| `UseDynamicVolume` | Permite o dimensionamento de posição baseado em portfólio (`Balance / BalanceToVolumeDivider`). |
| `BalanceToVolumeDivider` | Divisor aplicado ao valor do portfólio para emular `AccountFreeMargin / 10000`. |
| `MaxVolume` | Volume máximo de pedidos permitido pela estratégia. |

## Notas
- A estratégia usa `ClosePosition()` antes de abrir uma negociação na direção oposta, correspondendo à lógica MetaTrader que fecha ordens opostas via `CheckOrders`.
- Como os sinais são avaliados em velas finalizadas, as entradas podem ocorrer um pouco mais tarde do que a versão MetaTrader baseada em ticks. Essa mudança melhora a estabilidade ao executar backtests ou negociações ao vivo com dados de velas.
- Certifique-se de que o título selecionado forneça informações significativas de `PriceStep`, `VolumeStep` e avaliação de portfólio para que o bloco de volume dinâmico funcione conforme esperado.
