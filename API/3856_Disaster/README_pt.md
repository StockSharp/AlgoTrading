# Estratégia para Desastres (MQL #7704)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

O consultor especialista MetaTrader original chamado `disaster.mq4` arma ordens de stop em torno de uma média móvel simples muito longa (SMA). Ele espera até que o preço atual se afaste o suficiente da média e, em seguida, estaciona duas ordens de stop pendentes que tentam capturar um snap-back de reversão à média. Cada novo minuto recalcula o SMA e empurra os pedidos pendentes para a linha de base mais recente. As ordens preenchidas são protegidas por um stop-loss fixo e um take-profit adaptativo que diminui após a negociação anterior do mesmo lado ter fechado com perda.

## Notas de portabilidade

* **Fonte de dados** – o script MQL usa barras de 1 minuto até `iMA(PERIOD_M1, 590)`. A versão StockSharp assina uma série de velas configuráveis ​​(padrão `TimeSpan.FromMinutes(1)`) e alimenta um indicador `SMA` com o mesmo lookback.
* **Lógica de gatilho** – MetaTrader compara as cotações de compra/venda com SMA e requer um intervalo de 20 pip antes de armar uma ordem pendente. A porta C# reproduz isso convertendo o parâmetro `TriggerDistancePips` em uma distância de preço absoluta usando o instrumento `PriceStep`/`MinPriceStep` mais o multiplicador de 10× para símbolos FX de 3/5 dígitos.
* **Tipos de ordens** – o EA registra ordens de parada por meio de `OrderSend(..., OP_BUYSTOP/OP_SELLSTOP, ...)`. Os equivalentes de StockSharp são `BuyStop` e `SellStop`. A porta mantém ambas as ordens independentes, permitindo que qualquer uma delas permaneça ativa se as condições persistirem.
* **Relocação dinâmica** – sempre que uma nova vela chega o código MQL chama `OrderModify` para que as paradas pendentes rastreiem o novo SMA. StockSharp consegue o mesmo chamando `ReRegisterOrder` para mover pedidos ativos sem cancelar/recriar rotatividade.
* **Níveis de stop** – MetaTrader impõe níveis de stop do corretor (`MODE_STOPLEVEL`). A versão StockSharp respeita a mesma margem de segurança indiretamente, arredondando para a etapa de preço do instrumento e abortando a realocação quando o preço calculado for inválido (≤ 0).
* **Ordens de proteção** – no MT4 o stop-loss e o take-profit são anexados à ordem pendente. StockSharp cria ordens de proteção stop/limit separadas imediatamente após o preenchimento de uma entrada, refletindo as compensações de preço exatas.
* **Take-profit adaptativo** – o EA reduz pela metade a distância de take-profit para o próximo pedido se a negociação anterior desse lado perder dinheiro. A porta mantém sinalizadores `_lastBuyWasLoss` / `_lastSellWasLoss` e ajusta a distância de lucro de acordo.
* **Gerenciamento de dinheiro** – o script dimensiona os lotes com `0.4 * AccountFreeMargin / 1000`, limitado pelos limites do corretor. A porta StockSharp expõe um parâmetro `Volume` direto e o alinha com `VolumeStep`, `MinVolume` e `MaxVolume`.

## Parâmetros

| Parâmetro | Padrão | Descrição |
| --- | --- | --- |
| `Volume` | `0.1` | Volume do pedido alinhado à etapa de volume do instrumento. |
| `MaPeriod` | `590` | Comprimento médio móvel simples usado como linha de base. |
| `StopLossPips` | `30` | Distância entre o preço de entrada e o stop de proteção. |
| `TakeProfitPips` | `70` | Distância básica de lucro. Reduz automaticamente pela metade após uma negociação perdida do mesmo lado. |
| `TriggerDistancePips` | `20` | Intervalo necessário entre o preço e o SMA antes de armar as entradas de parada. |
| `CandleType` | `1-minute time frame` | Série de velas usadas para alimentar o SMA. |

Todos os parâmetros baseados em pip são traduzidos por meio do instrumento `PriceStep` ou `MinPriceStep`. Para pares FX com 3 ou 5 dígitos decimais, a conversão multiplica o passo por 10, correspondendo ao comportamento MetaTrader `Point`.

## Fluxo de trabalho

1. Assine as cotações do Nível 1 e velas de minuto.
2. Atualize os preços de compra/venda armazenados em cada mensagem de Nível 1.
3. Em cada vela concluída, recalcule o SMA e mova quaisquer pedidos pendentes ativos para a nova linha de base.
4. Se nenhuma posição estiver aberta e o gap de compra/venda exceder a distância de acionamento, coloque a ordem stop correspondente (venda acima de SMA, compre abaixo dele quando o preço estiver subvalorizado).
5. Quando uma ordem stop for preenchida, registre imediatamente ordens stop-loss e take-profit nas distâncias solicitadas. Acompanhe o último resultado comercial para adaptar o próximo lucro.
6. Cancele todas as ordens pendentes/protetoras quando a estratégia parar.

## Diferenças em relação à versão MQL

* A porta depende de StockSharp ordens de proteção em vez de campos SL/TP anexados ao corretor. O comportamento é equivalente, mas utiliza ordens explícitas na conta.
* MetaTrader impõe espaçamento de nível de parada com `MODE_STOPLEVEL`. StockSharp procura esse requisito arredondando para a etapa de preço disponível e ignorando atualizações quando o preço calculado for inválido. Na prática, deverá respeitar as mesmas restrições uma vez que o adaptador valide os preços dos pedidos.
* O código original recalcula o volume de negociação a partir da margem livre a cada tick. A porta StockSharp deixa o dimensionamento para o usuário por meio do parâmetro `Volume` para maior clareza e comportamento previsível entre corretores.

## Requisitos

* Os instrumentos devem expor pelo menos `PriceStep` ou `MinPriceStep`. Sem eles, a conversão pip-to-price cai para `0.0001`, o que é apropriado para os principais pares de FX.
* Para imitar as regras de nível de stop FX, o feed de dados deve fornecer as melhores atualizações de compra/venda (Nível 1). A estratégia se degrada normalmente usando o preço de fechamento da vela se faltarem cotações.
* As ordens de proteção exigem corretores/bolsas que suportem ordens de stop e limite. Se não estiver disponível, ajuste o código para voltar às saídas do mercado.

## Dicas de uso

* Comece com micro volumes (`0.01`) em contas de demonstração para validar as conversões de preços.
* Ajuste `TriggerDistancePips` e `TakeProfitPips` juntos: gatilhos menores levam a negociações mais frequentes, então considere reduzir o lucro de acordo.
* Monitore os sinalizadores `_lastBuyWasLoss` e `_lastSellWasLoss` por meio de registros para confirmar se a lógica adaptativa de obtenção de lucro corresponde ao histórico de MetaTrader.
