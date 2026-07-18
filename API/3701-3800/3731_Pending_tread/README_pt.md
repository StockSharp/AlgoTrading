# Estratégia de grade de piso pendente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de grade de piso pendente** é uma versão StockSharp fiel do MetaTrader 4 consultor especialista `Pending_tread.mq4`. O EA original reconstrói constantemente duas escadas de ordens pendentes: uma escada acima do mercado e outra abaixo. Cada escada pode ser configurada para usar ordens de compra ou venda e o espaçamento é definido em pips. A implementação StockSharp reproduz o mesmo comportamento por meio do API de alto nível sem introduzir indicadores ou coleções adicionais.

## Lógica de negociação
1. **Manutenção orientada por ofertas/vendas** – a estratégia assina cotações de nível 1 (`SubscribeLevel1`) e mantém os preços de compra e venda mais recentes. Cada vez que novos dados chegam, a rotina de manutenção é executada (com acelerador configurável) e compara as ordens pendentes existentes com o tamanho da grade configurado.
2. **Escada acima do mercado** – dependendo de `AboveMarketSide`, o algoritmo coloca ordens de compra stop ou de venda com limite em incrementos de `PipStep` pips acima do mercado. Cada novo pedido recebe seu próprio nível de lucro, compensado em `TakeProfitPips` pips.
3. **Escada abaixo do mercado** – o parâmetro `BelowMarketSide` seleciona entre ordens de limite de compra e ordens stop de venda empilhadas abaixo do mercado. Aplica-se o mesmo espaçamento de pip e lógica de lucro.
4. **Proteção de nível de parada** – o parâmetro `MinStopDistancePoints` emula a verificação MetaTrader `MODE_STOPLEVEL`. As ordens são ignoradas quando a distância entre o preço e a âncora de compra/venda relevante é menor que o limite fornecido.
5. **Aceleração** – `ThrottleSeconds` espelha a aceleração original de cinco segundos que evitou erros de `TRADE_CONTEXT_BUSY`. Apenas um ciclo de manutenção é executado durante esse intervalo, independentemente de quantos ticks chegam.

Todas as entradas baseadas em pip (`PipStep`, `TakeProfitPips`) são convertidas em compensações de preço absoluto usando os instrumentos `PriceStep` e `Decimals`. As cotações de cinco dígitos multiplicam automaticamente o passo por dez para corresponder à lógica do MetaTrader "ponto ajustado".

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `OrderVolume` | 0,01 | Volume utilizado ao colocar todas as ordens pendentes. Arredondado para a etapa de volume do instrumento antes do registro. |
| `PipStep` | 12 | Espaçamento entre ordens consecutivas na escada, expresso em pips. |
| `TakeProfitPips` | 10 | Distância em pips usada para colocar o lucro para cada ordem pendente. |
| `OrdersPerSide` | 10 | Número máximo de ordens ativas mantidas acima e abaixo do mercado. |
| `AboveMarketSide` | Comprar | Tipo de pedido usado acima do mercado. `Buy` cria ordens de parada de compra, `Sell` cria ordens de limite de venda. |
| `BelowMarketSide` | Vender | Tipo de pedido usado abaixo do mercado. `Buy` cria ordens de limite de compra, `Sell` cria ordens de stop de venda. |
| `MinStopDistancePoints` | 0 | Distância mínima (em pontos brutos) permitida entre o preço de compra/venda e o preço pendente. Defina isso para o corretor `MODE_STOPLEVEL` se necessário. |
| `ThrottleSeconds` | 5 | Período de resfriamento entre os ciclos de manutenção da rede. |
| `SlippagePoints` | 3 | Preservado para paridade de documentação; StockSharp pedidos pendentes não usam esse valor. |

## Notas de implementação
- Usa apenas os auxiliares de alto nível StockSharp (`SubscribeLevel1`, `BuyLimit`, `SellLimit`, `BuyStop`, `SellStop`).
- Os preços são normalizados por meio de `Security.ShrinkPrice` para que a corretora receba valores válidos alinhados aos ticks.
- O volume é ajustado para respeitar `VolumeStep`, `MinVolume` e `MaxVolume` antes de cada pedido ser enviado.
- Todas as mensagens de diagnóstico são roteadas por meio de `AddInfoLog` / `AddWarningLog`, espelhando a saída detalhada do script MetaTrader.
- A implementação do Python foi omitida intencionalmente, conforme solicitado.

## Dicas de uso
1. Atribua um instrumento e um portfólio líquidos e, em seguida, inicie a estratégia. As escadas pendentes aparecerão instantaneamente após a primeira atualização de nível 1.
2. Aumente `OrdersPerSide` com cautela: cada degrau adicional resulta em outra ordem pendente ativa no lado do corretor.
3. Para imitar o EA original com precisão, mantenha o acelerador padrão em cinco segundos e configure `MinStopDistancePoints` com o requisito de nível de stop do corretor.
4. Lembre-se de que StockSharp lida com posições líquidas; se escadas opostas forem acionadas simultaneamente, os preenchimentos resultantes compensarão parcialmente uns aos outros, em vez de criar subposições protegidas.
