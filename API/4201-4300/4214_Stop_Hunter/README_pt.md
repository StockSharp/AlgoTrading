# Pare a estratégia do caçador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Transfere o consultor especialista MetaTrader 4 **Stop Hunter** para a estrutura de estratégia de alto nível StockSharp.
- Concentra-se em quebras de números redondos: o algoritmo procura constantemente níveis de preços cujos dígitos `Zeroes` mais à direita são zero e coloca ordens de stop dentro desses limites.
- Mantém os níveis de take-profit e stop-loss ocultos do corretor, supervisionando as saídas internamente, reproduzindo o gerenciamento de risco "virtual" usado no EA original.
- Implementa a lógica de escalonamento de dois estágios do código-fonte: a primeira parte de uma posição é fechada após o alvo inicial, o restante segue o dobro da distância.

## Fluxo de dados e assinaturas
1. Assina dados de **Level1** (`SubscribeLevel1().Bind(ProcessLevel1)`) em `OnStarted`. Apenas o melhor fluxo de compra/venda é necessário; velas ou indicadores não são usados.
2. Cada atualização armazena o lance e o pedido mais recentes e aciona o mecanismo de decisão assim que a estratégia estiver online e a negociação for permitida.
3. Uma área de gráfico opcional é criada para visualizar as próprias negociações quando a estratégia é executada com os gráficos habilitados.

## Lógica de colocação de pedidos
- **Detecção de nível redondo**
  - Usa a etapa de preço do instrumento (`Security.PriceStep`) como o análogo MQL `Point`.
  - Calcula um comprimento de passo redondo: `roundStep = PriceStep * 10^Zeroes`.
  - Calcula o próximo número redondo acima do lance (`Math.Ceiling(bid / roundStep) * roundStep`).
  - Ajusta o nível quando o pedido já está dentro do buffer, espelhando a guarda original que evita o envio de ordens muito próximas do spread atual.
  - Deriva o nível de rodada inferior (`LevelS`) um passo abaixo de `LevelB` e executa o mesmo ajuste de segurança em relação ao lance.
- **Pedidos pendentes**
  - Coloca um **buy-stop** em `LevelB - DistancePoints * PriceStep` se nenhuma ordem existente estiver ativa, a negociação longa estiver habilitada e não houver nenhuma posição curta aberta.
  - Coloca um **sell-stop** simetricamente em `LevelS + DistancePoints * PriceStep` se a negociação curta for permitida e não existir posição longa.
  - Cancela ordens pendentes obsoletas sempre que a meta da rodada calculada avança ou o preço se afasta em mais de uma etapa de rodada mais `DistancePoints * 50`, correspondendo à lógica de limpeza da versão MQL.
  - Mantém o número total de slots ativos (posições + ordens pendentes) dentro de `MaxLongPositions + MaxShortPositions`.

## Gerenciamento de saída virtual
- Rastreia o preço médio de entrada e o volume da posição atual.
- Usa dois acumuladores inteiros (`_takeProfitExtension`, `_stopLossExtension`) para reproduzir os buffers ocultos originais:
  - Primeira meta de lucro: fecha metade da posição quando o bid/ask atinge `TakeProfitPoints * PriceStep` a favor da posição.
  - Após a primeira saída parcial, estende as distâncias de lucro e stop em mais `TakeProfitPoints`/`StopLossPoints`, ativando o estágio de "segunda negociação".
  - Saída final: fecha o volume restante quando a meta dobrada é atingida ou quando a distância de stop-loss dobrada é atingida.
- Fecha no mercado usando `BuyMarket` ou `SellMarket`, espelhando o EA que emitiu o mercado fecha em vez de ordens de stop loss do lado da corretora.
- Remove o stop pendente do lado oposto sempre que uma posição é aberta para evitar hedge, assim como o loop original que excluiu ordens conflitantes.

## Gestão de capital
- Reimplementa a função `Call_MM()` do EA: `volume = balance / 100000 * RiskPercent`.
- Fixa o volume calculado entre `MinimumVolume` e `MaximumVolume` e o arredonda para a etapa de volume do instrumento (ou para 2/1/0 decimais, dependendo de `MinimumVolume`).
- As saídas parciais reutilizam o tamanho da posição atual para calcular fechamentos de meio volume, respeitando a etapa de volume.

## Notas de implementação
- Usa apenas StockSharp APIs de alto nível (`BuyStop`, `SellStop`, `BuyMarket`, `SellMarket`, ligação de nível 1). Não são necessárias chamadas diretas de conectores ou coletas de indicadores.
- Mantém o estado interno durante as redefinições com `ResetState()` e garante que as guias sejam usadas para recuo de acordo com as diretrizes do repositório.
- Cláusulas de proteção (`IsFormedAndOnlineAndAllowTrading`) impedem o envio de pedidos antes que a estratégia seja totalmente inicializada.
- `OnOwnTradeReceived` espelha as verificações MQL que confirmaram fechamentos bem-sucedidos antes de atualizar o sinalizador `SecondTrade`.
- `OnOrderChanged` limpa referências para evitar identificadores obsoletos quando pedidos são cancelados ou rejeitados.

## Diferenças versus a versão MQL
- Modelo de compensação: as estratégias StockSharp operam com uma única posição líquida. Os parâmetros padrão ainda imitam o EA (um slot longo e um curto), mas a escalabilidade para vários tickets simultâneos não é suportada além da exposição líquida.
- A computação de risco usa `Portfolio.CurrentValue` (substituição para `BeginValue`) em vez de `AccountFreeMargin`, fornecendo uma aproximação portátil em ambientes com vários ativos.
- As distâncias virtuais de stop/take-profit são redefinidas de forma limpa quando uma nova negociação é aberta, evitando o bug de acumulação presente no código histórico EA.
- Todos os comentários e documentação estão escritos em inglês, enquanto os arquivos README descrevem adicionalmente a estratégia em russo e chinês, conforme exigido pelas diretrizes do projeto.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Zeroes` | 2 | Dígitos do lado direito que devem ser zero para que um preço seja considerado um nível redondo. |
| `DistancePoints` | 15 | Compensação (em faixas de preço) entre o nível redondo e a entrada de stop. |
| `TakeProfitPoints` | 15 | Distância de lucro oculta em pontos. Também reutilizado para a extensão do segundo estágio. |
| `StopLossPoints` | 15 | Distância de stop-loss oculta em pontos (dobrada após a primeira expansão). |
| `EnableLongOrders` | verdade | Permite a colocação de buy-stop. |
| `EnableShortOrders` | verdade | Permite a colocação de sell-stop. |
| `RiskPercent` | 5 | Percentual de capital utilizado para dimensionar as ordens pendentes. |
| `MinimumVolume` | 0,1 | Tamanho mínimo do pedido após arredondamento. |
| `MaximumVolume` | 30 | Limite para o volume calculado. |
| `MaxLongPositions` | 1 | Número máximo de slots longos (posição + pendente). |
| `MaxShortPositions` | 1 | Número máximo de slots curtos (posição + pendente). |

## Dicas de uso
1. Escolha um instrumento cuja etapa de preço esteja alinhada com a definição MQL `Point` usada pelo consultor especialista original. Pares Forex com pips fracionários normalmente requerem `Zeroes = 2`.
2. Monitore o tamanho do tick da corretora e o passo do volume; ajustar `MinimumVolume` garante que a lógica de arredondamento corresponda às restrições de câmbio.
3. Como as saídas são virtuais, mantenha sempre a estratégia online para evitar perder condições de stop-loss. Considere combinar com StockSharp de `StartProtection()` se o gerenciamento de risco do lado da bolsa for necessário.
4. Revise as variantes README russa e chinesa para obter explicações localizadas que os traders podem compartilhar com diferentes equipes.
