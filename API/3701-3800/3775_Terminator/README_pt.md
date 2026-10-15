# Estratégia do Exterminador do Futuro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia Terminator reproduz a lógica martingale baseada em grade do MetaTrader 4 consultor especialista "Terminator v2.0" usando o StockSharp API de alto nível. A estratégia entra na direção da inclinação MACD e então constrói uma cesta média sempre que o preço se move contra a posição por um número configurável de pips. A cesta é gerenciada com stop-loss opcional, take-profit, trailing stop e uma regra de proteção de lucro seguro que pode fechar a última negociação quando o lucro flutuante atingir uma meta.

## Lógica de negociação

1. **Geração de sinal** – Em cada vela finalizada a estratégia avalia o histograma MACD. Quando o valor MACD aumenta em comparação com o valor anterior, uma tendência de alta é assumida, enquanto uma diminuição de MACD indica uma tendência de baixa. Um sinalizador `ReverseSignals` pode inverter a interpretação.
2. **Entrada inicial** – Se não houver negociações abertas e o filtro de agendamento (`StartYear`, `StartMonth`, `EndYear`, `EndMonth`) permitir a negociação, a estratégia envia uma ordem de mercado na direção detectada, a menos que `ManualTrading` esteja habilitado.
3. **Martingale média** – Quando há uma cesta aberta, a estratégia espera que o preço se mova negativamente em `EntryDistancePips`. Cada entrada adicional duplica o volume anterior (ou multiplica-o por 1,5 se `MaxTrades` for maior que 12) até o limite de `MaxTrades`. O tamanho da posição também pode ser derivado do saldo da conta ativando `UseMoneyManagement`.
4. **Gerenciamento de riscos** –
   - **Take-profit**: `TakeProfitPips` define a distância usada para posicionar o nível de take-profit compartilhado.
   - **Parada inicial**: `InitialStopPips` define opcionalmente a parada de proteção inicial para a cesta completa.
   - **Trailing stop**: `TrailingStopPips` é ativado depois que a cesta ganha pelo menos a distância final mais um passo de espaçamento e, em seguida, move o stop na direção comercial.
   - **Proteção de conta**: quando `UseAccountProtection` está ativado e o número de negociações abertas atinge `MaxTrades - OrdersToProtect`, o lucro flutuante é comparado com `SecureProfit` (ou o valor atual do portfólio se `ProtectUsingBalance` for verdadeiro). Se o limite for excedido, a última negociação será fechada para garantir os ganhos e nenhuma nova entrada será permitida até que a cesta seja zerada.
5. **Reinicialização da cesta** – Quando a posição líquida retorna a zero, todos os contadores internos são zerados, permitindo um novo ciclo de negociação.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `TakeProfitPips` | Distância em pips para o nível de take-profit da cesta. |
| `InitialStopPips` | Distância de parada inicial em pips. Defina como zero para desativar. |
| `TrailingStopPips` | Distância de parada final em pips. Defina como zero para desativar. |
| `MaxTrades` | Número máximo de entradas de martingale permitidas simultaneamente. |
| `EntryDistancePips` | Movimento adverso mínimo necessário antes de adicionar a próxima negociação. |
| `SecureProfit` | Limite de lucro flutuante usado pelo módulo de proteção. |
| `UseAccountProtection` | Ativa o bloco de proteção de lucro seguro. |
| `ProtectUsingBalance` | Quando verdadeiro, o limite de proteção é igual ao valor atual do portfólio em vez de `SecureProfit`. |
| `OrdersToProtect` | Número de negociações finais assistidas pelo bloco de proteção (espelha a entrada original "Pedidos para Proteger"). |
| `ReverseSignals` | Inverte sinais de alta e baixa MACD. |
| `ManualTrading` | Desativa entradas automáticas mantendo ativa a gestão do cesto. |
| `LotSize` | Tamanho do lote fixo quando o gerenciamento de dinheiro está desativado. |
| `UseMoneyManagement` | Ativa o dimensionamento baseado em saldo derivado de `RiskPercent`. |
| `RiskPercent` | Percentagem de risco (por 100%) aplicada quando a gestão de dinheiro está ativa. |
| `IsStandardAccount` | Alterna entre escala de lote padrão e mini. |
| `EurUsdPipValue`, `GbpUsdPipValue`, `UsdChfPipValue`, `UsdJpyPipValue`, `DefaultPipValue` | Suposições de valor de pip usadas para converter pips em moeda para a regra de proteção. |
| `StartYear`, `StartMonth`, `EndYear`, `EndMonth` | Restrinja o intervalo de tempo em que novas cestas podem ser abertas. |
| `CandleType` | Período usado para construir o sinal MACD. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | Configurações de período do indicador MACD. |

## Notas de uso

- A estratégia segue o tipo de vela definido por `CandleType` e reage apenas às velas finalizadas.
- Para espelhar o comportamento original do MT4, certifique-se de que os parâmetros do valor pip do símbolo correspondam às especificações da sua corretora.
- Quando `ManualTrading` estiver ativado, você ainda poderá gerenciar pedidos manualmente; o algoritmo continuará seguindo os limites e aplicando a proteção da conta na cesta aberta.
- A implementação se concentra no método de entrada baseado em MACD do consultor especialista original porque os outros modos dependiam de indicadores personalizados que não estão disponíveis em StockSharp.

## Detalhes da conversão

- Gerenciamento de dinheiro, espaçamento de pip, escala de martingale e lógica de lucro seguro seguem a estrutura original do código MQ4.
- As opções MT4 `AccountProtection` e `AllSymbolsProtect` são combinadas nos parâmetros `UseAccountProtection` e `ProtectUsingBalance`.
- `ReverseCondition` e `Manual` sinalizadores do mapa de origem para `ReverseSignals` e `ManualTrading` respectivamente.
- As regras de stop loss e trailing operam na cesta agregada e não por pedido, semelhante ao comportamento do consultor especialista de origem.

## Como correr

1. Abra a solução no Visual Studio.
2. Adicione a estratégia a uma instância `StrategyRunner` ou `StrategyConnector`.
3. Configure os parâmetros na UI ou por meio de código.
4. Inicie a estratégia; ele assinará automaticamente a série de velas especificada e começará a avaliar os sinais.
