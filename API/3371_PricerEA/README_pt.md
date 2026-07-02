# Estratégia PricerEA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia PricerEA** recria o comportamento do MetaTrader 4 especialista "PricerEA v1.0" usando o StockSharp API de alto nível.
Ele coloca até quatro ordens pendentes (stop de compra, stop de venda, limite de compra e limite de venda) em níveis de preços definidos manualmente. Uma vez qualquer
ordem pendente é preenchida, a estratégia anexa ordens protetoras de stop-loss e take-profit, habilitando opcionalmente um trailing stop e
ajuste de ponto de equilíbrio para seguir o Expert Advisor original.

## Como funciona

1. **Ordens pendentes** – a estratégia lê os níveis de preços absolutos das entradas e envia apenas as ordens pendentes correspondentes
uma vez na inicialização. A expiração opcional pode ser configurada em minutos.
2. **Seleção de volume** – os usuários podem manter o tamanho de lote manual fixo ou mudar para o modo automático de onde o volume é derivado
o saldo da carteira e o análogo do fator de risco MT4.
3. **Proteção** – depois que uma ordem de entrada é preenchida, a estratégia cria ordens de stop-loss e take-profit na distância configurada
(expresso em faixas de preço). Quando o trailing e o ponto de equilíbrio estão ativados, o stop segue as condições originais MQL: é
movido somente depois que o preço cobriu a distância do ponto de equilíbrio mais o stop inicial.
4. **Manutenção de pedidos** – os pedidos pendentes são cancelados automaticamente quando seu tempo de vida expira ou quando a estratégia é interrompida.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `BuyStopPrice`, `SellStopPrice`, `BuyLimitPrice`, `SellLimitPrice` | Preços absolutos para as ordens pendentes correspondentes. Um valor de `0` desativa o pedido. |
| `TakeProfitPoints` | Distância do preço de entrada até a ordem take-profit, medida em faixas de preço (`Security.PriceStep`). |
| `StopLossPoints` | Distância do preço de entrada até a ordem stop-loss, também medida em pontos de preço. |
| `EnableTrailingStop` | Ativa a lógica de trailing stop. |
| `TrailingStepPoints` | Movimento mínimo (em pontos) necessário antes do stop móvel ser movido. |
| `EnableBreakEven` | Ativa a regra de ponto de equilíbrio que eleva o stop acima/abaixo da entrada após lucro suficiente. |
| `BreakEvenTriggerPoints` | Lucro extra (pontos) necessário antes que o stop seja movido para atingir o ponto de equilíbrio. |
| `PendingExpiryMinutes` | Vida útil das ordens pendentes em minutos. `0` os mantém ativos até serem preenchidos ou cancelados manualmente. |
| `VolumeMode` | Escolhe entre volume manual e dimensionamento automático. |
| `RiskFactor` | Multiplicador de risco usado pelo dimensionamento automático (espelha a entrada MQL). |
| `ManualVolume` | Tamanho de lote fixo usado quando `VolumeMode` é definido como `Manual`. |

## Diferenças versus a versão MT4

- O cálculo automático do volume utiliza o saldo do portfólio StockSharp e o multiplicador do contrato de segurança. Corretores diferentes
pode usar fórmulas distintas, portanto o valor resultante pode ser ligeiramente diferente de MetaTrader.
- As ordens de proteção são feitas por meio de ajudantes StockSharp e respeitam a etapa de volume do local, volume mínimo e máximo.
- A expiração é implementada dentro da estratégia (MetaTrader depende da expiração do pedido no lado do servidor).

## Notas de uso

- Configure os níveis de preços antes de iniciar a estratégia. Valores iguais a zero deixam a ordem correspondente desabilitada.
- Para imitar a lógica de "dígitos" do MT4, os parâmetros baseados em pontos operam em unidades `Security.PriceStep`.
- Combine a estratégia com o portfólio e ferramentas de registro de StockSharp para monitorar ordens pendentes e paradas protetoras.
