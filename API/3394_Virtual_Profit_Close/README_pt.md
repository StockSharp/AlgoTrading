# Estratégia de fechamento de lucro virtual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Virtual Profit Close replica o comportamento do consultor especialista MetaTrader 4 *Virtual_Profit_Close.mq4*. A estratégia observa o
posição atual do título configurado e sai assim que uma meta de lucro virtual é atingida. Ao contrário de uma ordem de lucro regular,
o nível de saída é avaliado internamente para que nenhuma ordem de lucro seja deixada na carteira de pedidos. Um trailing stop configurável pode mover a saída
preço mais próximo do mercado à medida que a negociação se transforma em lucro. Ao executar no modo de teste, a estratégia pode abrir automaticamente posições de amostra
para demonstrar sua lógica.

## Notas de conversão

- Os eventos de tick são consumidos por meio de `SubscribeTrades().Bind(ProcessTrade).Start()` para imitar a rotina `OnTick` original.
- MetaTrader "pontos" são convertidos em pips inspecionando `Security.PriceStep` e ajustando para símbolos de 3/5 dígitos.
- O lucro virtual e os cálculos finais usam o lance atual para posições longas e o pedido para posições curtas, correspondendo ao MQL
implementação que dependia de preços `Bid` e `Ask`.
- A lógica de trailing stop é ativada após o limite de lucro configurado e mantém o stop a uma distância fixa do mercado
preço, semelhante a chamar repetidamente `OrderModify` em MQL.
- Um modo de demonstração substitui o auxiliar original do testador de estratégia (`SendTest`) abrindo ordens de mercado de acordo com o selecionado
direção e volume. Paradas de proteção opcionais são colocadas usando `SetStopLoss`.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `ProfitPips` | Nível de lucro virtual expresso em MetaTrader pips. A estratégia fecha a posição quando o lucro ultrapassa esta distância. |
| `UseTrailingStop` | Ativa o comportamento de rastreamento quando definido como `true`. |
| `TrailingOffsetPips` | Distância mantida entre o preço atual e o trailing stop quando estiver ativo. |
| `TrailingActivationPips` | Lucro mínimo em pips exigido antes do trailing stop ser ativado. |
| `EnableDemoMode` | Abre automaticamente ordens de demonstração cada vez que a posição fica plana. Útil para backtests. |
| `DemoOrderDirection` | Direção dos pedidos de demonstração (`Buy` ou `Sell`). |
| `DemoOrderVolume` | Volume enviado para pedidos de demonstração. |
| `DemoStopPips` | Parada de proteção opcional para pedidos de demonstração, expressa em pips. |

## Comportamento

1. Quando a estratégia é iniciada, ela calcula o tamanho do pip e as distâncias para lucro, trailing e demo stops.
2. Cada tick recebido por meio de `ProcessTrade` avalia a posição atual:
   - As posições longas são fechadas quando o preço de compra entrega o lucro virtual configurado.
   - As posições curtas são fechadas quando o preço de venda percorre a mesma distância na direção oposta.
3. Se o trailing estiver habilitado e o limite de ativação for atingido, o trailing stop se move junto com o movimento favorável do preço. Uma vez
o mercado cruza o nível final, a estratégia envia uma ordem de mercado para sair.
4. O modo de demonstração pode abrir automaticamente uma nova posição sempre que a estratégia se torna plana, recriando o recurso exclusivo para testador do
especialista original.

## Requisitos

- A estratégia precisa de dados de mercado ao nível dos ticks para responder com precisão às mudanças de preços.
- Apenas um símbolo deve ser atribuído à instância da estratégia. Vários símbolos simultâneos não são suportados, correspondendo ao original
Implementação MQL que monitorou o símbolo do gráfico atual.
