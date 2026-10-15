# Estratégia de Engolfo Simples
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Simple Engulfing** replica o comportamento dos MetaTrader 4 especialistas "simple engulf mt4 buy" e "simple engulf mt4 sell". Ambos os especialistas detectam padrões de velas envolventes e abrem negociações em uma única direção. A porta StockSharp mescla ambos os consultores em uma estratégia configurável para que o trader possa reproduzir o comportamento original somente de compra, somente venda ou combinado dentro da estrutura StockSharp.

A estratégia escuta apenas velas concluídas, o que corresponde ao estilo de execução de fechamento de barra usado pela versão MetaTrader. Todas as colocações de pedidos usam StockSharp API de alto nível (`SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket` e `StartProtection`) para ficar próximo às diretrizes de codificação de StockSharp.

## Lógica de negociação
1. Construa velas com base no `CandleType` configurado.
2. Aguarde o término da vela atual e mantenha a vela anterior concluída na memória.
3. Calcule o tamanho atual do corpo da vela em pips. Rejeite o padrão quando estiver abaixo de `MinBodyPips` ou acima de `MaxBodyPips` (se o filtro máximo estiver habilitado com um valor positivo).
4. Detecte um padrão de **engolfo de alta** quando:
   - A vela anterior é de baixa (fecha abaixo da abertura).
   - A vela atual é de alta (fechamento acima da abertura).
   - A abertura atual está abaixo ou igual ao fechamento anterior.
   - O fechamento atual está acima ou igual à abertura anterior.
5. Detecte um padrão de **engolfo de baixa** usando as condições espelhadas.
6. Quando um padrão válido aparecer, certifique-se de que a negociação automatizada seja permitida (`IsFormedAndOnlineAndAllowTrading()`) e que a direção configurada permita a negociação:
   - `BuyOnly` replica o robô "simple engulf mt4 buy".
   - `SellOnly` replica o robô "simple engulf mt4 sell".
   - `Both` permite negociação bidirecional.
7. Use o `TradeVolume` configurado para cada entrada. Se a estratégia estiver atualmente posicionada no lado oposto, ela fecha a posição e vira adicionando o tamanho absoluto da posição à ordem de entrada, correspondendo ao comportamento MetaTrader ao mudar de curto para longo (ou vice-versa).
8. Os níveis opcionais de stop-loss e take-profit são aplicados por meio de `StartProtection` usando unidades baseadas em preço. Eles convertem as distâncias do pip em incrementos de preço do instrumento para que StockSharp gerencie as ordens de proteção da mesma forma que os especialistas originais.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | `TimeFrame(15 minutes)` | Tipo de vela e intervalo de agregação usado para detectar padrões. |
| `TradeVolume` | `0.01` | Volume de pedidos por entrada, idêntico ao dos especialistas MetaTrader. |
| `StopLossPips` | `20` | Distância de stop-loss expressa em pips. Defina como `0` para desativar a ordem de proteção. |
| `TakeProfitPips` | `20` | Distância de lucro expressa em pips. Defina como `0` para desativar a ordem de proteção. |
| `MinBodyPips` | `0` | Corpo mínimo da vela (em pips) necessário para um padrão de engolfamento válido. |
| `MaxBodyPips` | `50` | Corpo máximo da vela (em pips) permitido para um padrão de engolfamento válido. Use `0` para remover o filtro superior. |
| `Direction` | `BuyOnly` | Define quais lados dos orientadores originais devem ser executados (`BuyOnly`, `SellOnly` ou `Both`). |

## Notas práticas
- O tamanho do pip se adapta automaticamente ao instrumento negociado, analisando o `PriceStep` do instrumento e o número de casas decimais. Isso garante que os filtros pip e as ordens de proteção se comportem como as entradas MetaTrader em símbolos Forex de 4 e 5 dígitos.
- Ordens de proteção são enviadas somente quando `StopLossPips` ou `TakeProfitPips` são positivos. Caso contrário, a estratégia deixa saídas para a gestão discricionária ou outros módulos de automação.
- Como a estratégia espera por velas totalmente finalizadas, os sinais são gerados no fechamento de cada barra, evitando a repintura intra-barra.
- Chamadas API de alto nível mantêm a implementação concisa e seguem a diretriz do projeto de preferir componentes StockSharp prontos em vez do processamento manual de pedidos.

## Diferenças do Original
- Ambos os consultores MetaTrader são combinados em uma única estratégia com um parâmetro `Direction` em vez de dois arquivos separados.
- Auxiliares de registro e gráficos de StockSharp (velas opcionais e gráficos comerciais) são adicionados para melhor visibilidade ao executar dentro de terminais StockSharp.
- O gerenciamento de risco usa o auxiliar `StartProtection` do StockSharp, que gerencia internamente ordens de stop-loss e take-profit por meio do mecanismo StockSharp. O comportamento resultante é equivalente ao uso de paradas bruscas em MetaTrader.
