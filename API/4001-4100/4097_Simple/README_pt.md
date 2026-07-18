# Estratégia Simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Simples** é a StockSharp conversão de alto nível do MetaTrader 4 consultor especialista `S!mple.mq4` localizado em `MQL/9019`. O sistema original monitorava uma cesta fixa de símbolos Forex e negociava sempre que uma média móvel ponderada linear de 50 períodos cruzava uma média móvel simples de 200 períodos. Cada entrada pode ser repetida um número configurável de vezes e níveis opcionais de stop-loss e take-profit baseados em dinheiro foram anexados a cada negociação. A conversão mantém a mesma lógica, expõe todas as entradas do usuário como parâmetros de estratégia e registra as mesmas informações de diagnóstico que o EA imprimiu no comentário do terminal MetaTrader.

## Lógica de negociação
1. **Preparação de dados.** A estratégia assina um tipo de vela configurável (velas de cinco minutos por padrão) e vincula ambas as médias móveis por meio do `SubscribeCandles().Bind(...)` API de alto nível.
2. **Cruzamento de média móvel.** Dois valores históricos de cada média móvel são armazenados em buffer. Um sinal de compra ocorre quando o LWMA rápido estava abaixo do lento SMA duas barras atrás e fechou acima dele na barra finalizada anterior. Um sinal de venda é detectado quando a condição inversa acontece.
3. **Acompanhamento de margem de tendência.** O valor lento de SMA que ocorreu há `TrendMargin` barras é armazenado em cache para reproduzir o relatório de tendência textual de EA. A lentidão ao vivo SMA é comparada com essa referência para classificar a tendência de fundo como `UP`, `DOWN` ou `WAIT`, juntamente com a distância expressa em etapas de preço.
4. **Modelo de execução.**
   - Quando um sinal de compra é acionado, qualquer exposição curta é fechada antes da compra até `NumOrders * TradeVolume`. O volume solicitado reflete o comportamento EA em que vários pedidos idênticos foram empilhados até que a contagem máxima fosse atingida.
   - Um sinal de venda fecha primeiro a exposição longa e depois vende até o mesmo volume alvo agregado.
5. **Níveis de proteção.** Stops e metas opcionais baseados em dinheiro (`StopLossMoney`, `TakeProfitMoney`) são traduzidos em distâncias de preço usando o instrumento `PriceStep`/`StepPrice` e o por pedido `TradeVolume`. Uma vez armazenados os níveis, cada vela finalizada verifica a faixa máxima/mínima; se um nível for ultrapassado, a posição será achatada no mercado.
6. **Guarda operacional.** A colocação real do pedido é executada somente quando `EnableTrading` é definido como `true`, replicando o sinalizador `makeTrades` original que permite que o EA seja executado em um modo "somente análise".

## Gestão de riscos e paradas de dinheiro
- Os valores de stop-loss e take-profit são interpretados como risco/alvo de caixa por bloco de entrada (por pedido MetaTrader). A conversão usa os metadados de segurança (`PriceStep`, `StepPrice`) para converter esse valor em um número arredondado de etapas de preço. Se algum dos campos estiver faltando, um aviso será registrado e as paradas monetárias permanecerão desabilitadas.
- Os níveis de proteção são avaliados no máximo/mínimo de cada vela concluída, correspondendo às verificações de nível de tick feitas pelo EA enquanto permanece dentro da estrutura de alto nível do StockSharp.
- `StartProtection()` é invocado no início para que as proteções no nível da conta configuradas em StockSharp permaneçam ativas enquanto a estratégia é executada.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volume de um único pedido do tipo MetaTrader. A base `Strategy.Volume` é mantida sincronizada com este valor. |
| `NumOrders` | `1` | Número máximo de blocos de volume que podem ser acumulados na mesma direção. O volume de destino final é igual a `TradeVolume * NumOrders`. |
| `StopLossMoney` | `0` | Valor de stop-loss opcional na moeda da conta por bloco de volume. Defina como zero para desativar a parada. |
| `TakeProfitMoney` | `0` | Valor opcional de obtenção de lucro na moeda da conta por bloco de volume. Defina como zero para desabilitar o alvo. |
| `TrendMargin` | `10` | Número de velas finalizadas usadas para produzir o texto da tendência de fundo (lento SMA em comparação com seu valor há `TrendMargin` barras atrás). |
| `FastLength` | `50` | Comprimento da média móvel ponderada linear rápida. |
| `SlowLength` | `200` | Comprimento da média móvel simples lenta. |
| `EnableTrading` | `false` | Quando `false` a estratégia registra apenas sinais, exatamente como EA quando `makeTrades=false`. |
| `CandleType` | `5m time-frame` | Tipo de vela usado para cálculos de indicadores. |

## Notas sobre a conversão
- O MetaTrader EA iterou por meio de seis símbolos Forex codificados. As estratégias StockSharp operam no `Strategy.Security` fornecido pelo usuário. Para reproduzir o comportamento de negociação em cesta, lance várias instâncias da estratégia (uma por instrumento) ou envolva-as em uma estratégia pai que envia os mesmos sinais para vários títulos.
- Os níveis de proteção baseados em dinheiro dependem dos metadados do instrumento. Para pares Forex, certifique-se de que `PriceStep` e `StepPrice` estejam preenchidos (por exemplo, `0.0001` e o valor do pip por lote). Caso contrário, a distância parada/alvo será tratada silenciosamente como zero após registrar um aviso.
- A mensagem de registro emitida em cada vela finalizada reflete o comentário EA: ela lista o sinal (`BUY`, `SELL` ou `WAIT`), ambas as médias móveis, a distância entre elas em etapas de preço e a avaliação de tendência obtida da lenta atrasada SMA.
- O número de pedidos empilhados é modelado como um volume alvo agregado. Isso mantém a exposição total idêntica à implementação original ao usar os auxiliares de ordem de mercado de alto nível de StockSharp em vez de várias chamadas `OrderSend` individuais.
- Nenhuma porta Python foi criada ainda, correspondendo aos requisitos da tarefa.

## Dicas de uso
- Atribua uma segurança Forex com valores `PriceStep`, `StepPrice` e `VolumeStep` configurados corretamente. Defina `TradeVolume` para o tamanho de lote desejado e ative a negociação quando estiver satisfeito com o diagnóstico registrado.
- Para imitar o comportamento padrão EA (somente análise), deixe `EnableTrading` em `false`. Quando estiver pronto para negociar, mude para `true` e o próximo sinal cruzado enviará ordens de mercado.
- Como os níveis de proteção são monitorados no fechamento das velas, considere usar velas mais curtas se precisar de uma reação intrabarra mais rígida em comparação com o comportamento tick-by-tick de MetaTrader.
