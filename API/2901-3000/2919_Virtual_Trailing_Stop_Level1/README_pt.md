# Estratégia de Stop Trailing Virtual Level1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Stop Trailing Virtual** é uma conversão direta do consultor especialista do MetaTrader `Virtual Trailing Stop.mq5` (MQL ID 21362). O especialista original apenas gerencia stops de proteção para posições que foram abertas em outro lugar. Este port em C# reproduz o mesmo comportamento sobre a API de alto nível do StockSharp: monitora as melhores cotações de bid/ask e fecha a posição atual quando as condições de stop-loss, take-profit ou trailing stop são atendidas.

Ao contrário das estratégias orientadas a entrada, esta implementação nunca abre novas posições por conta própria. Ela foi projetada para ser combinada com outras entradas automatizadas ou sessões de negociação manual quando é necessário aplicar um trailing stop "virtual" no estilo MetaTrader dentro do StockSharp.

## Lógica de negociação
1. **Feed Level1** – a estratégia assina dados de nível 1 e armazena continuamente os últimos valores de bid/ask.
2. **Conversão de pips** – as entradas do usuário são definidas em *pips*. A estratégia as converte em deslocamentos de preço multiplicando o valor pelo `PriceStep` do instrumento. Para cotações forex de 3 e 5 dígitos, um multiplicador de 10x é aplicado para corresponder à definição de pip do MetaTrader.
3. **Verificação do stop-loss** – se o bid de uma posição comprada cair abaixo de `PreçoEntrada − StopLoss`, ou o ask de uma posição vendida subir acima de `PreçoEntrada + StopLoss`, a posição é fechada a mercado.
4. **Verificação do take-profit** – se o bid de uma posição comprada subir acima de `PreçoEntrada + TakeProfit`, ou o ask de uma posição vendida cair abaixo de `PreçoEntrada − TakeProfit`, a posição é fechada.
5. **Ativação do trailing** – assim que o preço se move `TrailingStart` pips a favor da posição, um nível de trailing é criado em `Bid − TrailingStop` (comprado) ou `Ask + TrailingStop` (vendido).
6. **Atualização do trailing** – cada vez que o lucro não realizado aumenta pelo menos `TrailingStep` pips, o nível de trailing é deslocado adequadamente. Definir o step como zero faz o trailing seguir cada tick favorável.
7. **Saída por trailing** – a posição é fechada quando o preço toca o nível de trailing enquanto a negociação permanece lucrativa (espelhando a salvaguarda `Profit()>0` do EA fonte).

Nenhuma ordem pendente é colocada. Cada saída é executada por meio de ordens de mercado para imitar a natureza "virtual" da implementação MQL.

## Parâmetros
| Parâmetro | Descrição | Valor padrão |
| --- | --- | --- |
| `StopLossPips` | Distância do stop-loss em pips. Definir como `0` para desativar o gerenciamento de stop-loss fixo. | `0` |
| `TakeProfitPips` | Distância do take-profit em pips. Definir como `0` para desativar o gerenciamento de take-profit. | `0` |
| `TrailingStopPips` | Distância entre o preço atual e o nível de trailing, medida em pips. | `5` |
| `TrailingStartPips` | Limite de lucro (em pips) que deve ser atingido antes que o trailing seja ativado. | `5` |
| `TrailingStepPips` | Aumento mínimo em pips necessário antes que o nível de trailing seja movido novamente. Usar `0` para trailing contínuo. | `1` |

Todos os parâmetros suportam otimização graças aos helpers `StrategyParam` do StockSharp.

## Notas de implementação
- A estratégia usa apenas dados de nível 1 (`DataType.Level1`) e não registra objetos de gráfico porque o StockSharp lida com a visualização de forma diferente do MetaTrader.
- As conversões de preço dependem de `Security.PriceStep` e `Security.Decimals`. Se a bolsa não fornecer esses metadados, o tamanho de pip de fallback é `1`.
- A proteção é simétrica para posições compradas e vendidas. Os valores de trailing são armazenados separadamente para ambas as direções.
- A inicialização automática de posições que existia no modo tester dentro do EA original foi intencionalmente omitida porque as estratégias do StockSharp operam sobre posições líquidas.

## Dicas de uso
- Anexe a estratégia a um par carteira/instrumento que já tenha posições abertas ou que se espera receber de outro componente.
- Combine-a com negociação discricionária ou estratégias de entrada automatizadas para emular o gerenciamento de negociações no estilo MetaTrader no StockSharp Designer, Shell ou Runner.
- Ao negociar instrumentos não-forex, ajuste as entradas baseadas em pips para corresponder ao tamanho de tick do instrumento. Definir `TrailingStopPips = 1` efetivamente faz o trailing de um `PriceStep`.

## Arquivos
- `CS/VirtualTrailingStopLevel1Strategy.cs` – implementação da estratégia.
- `README.md`, `README_zh.md`, `README_ru.md` – documentação multilíngue da estratégia.
