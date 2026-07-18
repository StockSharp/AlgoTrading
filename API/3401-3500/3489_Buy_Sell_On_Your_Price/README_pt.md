# Estratégia BuySellOnYourPrice
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Converte o consultor especialista MetaTrader **BuySellonYourPrice.mq5** (id 35391) em StockSharp API de alto nível.
- Envia exatamente uma ordem no início, correspondendo à lógica original que não requer ordens ou posições ativas.
- Suporta entradas de mercado, limite e stop com níveis opcionais de stop-loss e take-profit expressos como preços absolutos.
- Configura automaticamente StockSharp ordens de proteção quando distâncias válidas de stop-loss/take-profit podem ser derivadas dos níveis de preços fornecidos.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Mode` | Tipo de pedido a ser enviado (Nenhum, Compra, Venda, BuyLimit, SellLimit, BuyStop, SellStop). | `None` |
| `OrderVolume` | Volume para o pedido gerado. | `1` |
| `EntryPrice` | Preço utilizado para ordens pendentes; ignorado para ordens de mercado. | `0` |
| `StopLossPrice` | Nível de preço absoluto para o stop loss. | `0` |
| `TakeProfitPrice` | Nível de preço absoluto para o take-profit. | `0` |

## Lógica de negociação
1. Quando a estratégia é iniciada ela verifica se:
   - Um `Mode` válido diferente de `None` é selecionado.
   - `OrderVolume` é positivo.
   - Não há posição atual nem ordens ativas. Se algum deles estiver presente, o pedido não será enviado (o mesmo que `OrdersTotal()==0` e `PositionsTotal()==0` verificar em MQL).
2. O preço de entrada é resolvido:
   - Os modos de mercado usam o melhor bid/ask, voltando ao último preço ou `EntryPrice` quando nenhum dado de mercado está disponível ainda.
   - Os modos pendentes requerem `EntryPrice > 0`.
3. As distâncias de proteção são derivadas dos níveis especificados de stop-loss e take-profit. Somente distâncias positivas e válidas são passadas para `StartProtection` para emular os parâmetros EA.
4. O tipo de pedido selecionado é enviado (`BuyMarket`, `SellLimit`, `BuyStop`, etc.) exatamente uma vez e registros informativos são produzidos para refletir a ação.

## Diferenças do original EA
- O registro é executado por meio de `AddInfoLog` em vez de `Print`.
- As ordens de proteção são registradas via `StartProtection` quando tanto o preço de entrada quanto o stop-loss/take-profit permitem calcular uma distância positiva.
- A resolução do preço de mercado usa dados atuais do Nível 1 (`BestBid`, `BestAsk`, `LastPrice`) e adia o envio do pedido se nenhuma cotação ainda estiver disponível.

## Notas de uso
- Atribua a segurança desejada antes de iniciar a estratégia e garanta que os dados do Nível 1 estejam disponíveis para ordens de mercado.
- Defina `EntryPrice`, `StopLossPrice` e `TakeProfitPrice` em termos absolutos ao usar pedidos pendentes.
- Deixe `Mode` como `None` para desabilitar a negociação sem remover a estratégia do ambiente.
