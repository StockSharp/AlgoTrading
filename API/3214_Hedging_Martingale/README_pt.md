# Estratégia de Hedging Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port StockSharp do consultor especialista MetaTrader "Hedging Martingale" (pasta `MQL/23693`). Mantém um hedge equilibrado abrindo tanto uma posição comprada quanto uma vendida em cada nova barra e então aplica um esquema de averaging martingale. Quando o preço se move adversamente por uma distância de pip configurável, a estratégia adiciona uma nova posição no lado perdedor com volume aumentado enquanto mantém o hedge oposto. O lucro flutuante é gerenciado usando alvos baseados em dinheiro e percentual juntamente com um bloqueio de trailing opcional.

## Lógica de trading
- **Hedge inicial**: sempre que a estratégia estiver flat e uma nova vela fechar, ela compra e vende simultaneamente usando o mesmo volume base.
- **Passos de martingale**: se o preço se mover contra um lado por `Pip Step` pips, uma ordem adicional é aberta nesse lado. O volume é multiplicado por `Volume Multiplier`, emulando o dimensionamento de lote progressivo da versão MQL. O lado oposto permanece aberto para manter o hedge.
- **Take-profit por negociação**: cada entrada aberta tem uma distância de take-profit individual definida por `Take Profit (pips)`. Quando o mercado se move a favor de uma perna por essa distância, a perna é reduzida emitindo uma ordem de compensação.
- **Saídas de cesta**: o conjunto completo de posições pode ser fechado quando o lucro flutuante atinge um alvo monetário, uma percentagem do capital inicial, ou após um bloqueio de trailing devolver mais do que o recuo permitido. Esses comportamentos replicam `Take_Profit_In_Money`, `Take_Profit_In_percent` e `TRAIL_PROFIT_IN_MONEY2` do especialista original.
- **Limites de negociação**: o parâmetro `Max Trades` restringe quantos passos de martingale podem estar ativos. Se `Close On Max` estiver habilitado, a cesta é liquidada assim que o limite for excedido.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| Candle Type | Período que impulsiona a lógica. Cada vela concluída pode acionar novas ações de hedge. |
| Use Money TP / Money Take Profit | Habilitar e definir o lucro flutuante (em unidades de moeda) que fecha todas as posições. |
| Use Percent TP / Percent Take Profit | Fechar a cesta quando o lucro flutuante atingir uma percentagem do valor inicial do portfólio. |
| Enable Trailing / Trailing Start / Trailing Step | Ativar o bloqueio de trailing baseado em dinheiro para a cesta e configurar o nível de gatilho juntamente com o recuo de lucro permitido. |
| Take Profit (pips) | Distância em pips para saídas de take-profit por perna. |
| Pip Step | Movimento adverso de preço (em pips) necessário antes de adicionar outra ordem de martingale. |
| Base Volume | Volume inicial para as pernas de compra e venda. |
| Volume Multiplier | Multiplicador aplicado ao maior volume de posição ao adicionar entradas de martingale. |
| Max Trades | Número máximo de entradas abertas simultaneamente (em ambas as direções). |
| Close On Max | Se liquidar todas as posições assim que a contagem máxima de negociações for excedida. |

## Notas
- A estratégia usa `BuyMarket` e `SellMarket` para todos os posicionamentos de ordens, refletindo o modelo de execução de mercado do especialista fonte.
- Os valores de volume são normalizados para o passo de lote do instrumento para evitar ordens rejeitadas.
- Quando a estratégia fica flat, o bloqueio de trailing é redefinido para que novas cestas comecem com uma referência de lucro limpa.

## Arquivos
- `CS/HedgingMartingaleStrategy.cs` – implementação da estratégia convertida (C#).
- `README.md` – esta documentação (inglês).
- `README_zh.md` – tradução chinesa.
- `README_ru.md` – tradução russa.
