# Estratégia SwingTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia SwingTrader** é uma versão StockSharp do MetaTrader 4 consultor especialista `SwingTrader.mq4`. O EA original procura
Bollinger Reversões de banda: quando o preço salta da banda externa e a próxima barra cruza a linha média, o consultor abre uma
posição e começa a construir uma grade média no estilo martingale. A estratégia traduzida reproduz o mesmo comportamento de alto nível
usando velas StockSharp, Bollinger bandas de `StockSharp.Algo.Indicators` e os auxiliares de pedido da estrutura (`BuyMarket`,
`SellMarket`). A escala de volume, a largura da grade e as regras de liquidação refletem o código MT4, respeitando a troca
limites fornecidos pelos metadados `Security`.

## Lógica de negociação
1. Assine o período configurado (`CandleType`) e alimente um indicador de bandas Bollinger com comprimento `BollingerPeriod` e um
multiplicador de desvio padrão fixo de `2`.
2. Trabalhe apenas com velas prontas; o retorno de chamada do indicador ignora barras parcialmente formadas para replicar o MT4 `IsNewCandle()`
guarda.
3. Acompanhe se a vela anterior tocou a banda superior ou inferior. O par booleano `_upTouch` / `_downTouch` segue o
lógica de alternância original que mantém apenas um lado ativo até que a banda oposta seja tocada.
4. Quando nenhuma cesta estiver aberta:
   - abra uma posição longa se a última barra completa cruzar acima da faixa do meio depois de tocar anteriormente na faixa inferior;
   - abra uma posição curta se a barra cruzou abaixo da banda do meio depois de tocar a banda superior.
O volume de primeira ordem é igual a `InitialVolume` (após arredondamento de câmbio) e a largura inicial da grade é igual à última distância
entre as bandas Bollinger superior e inferior.
5. Quando existir uma cesta, observe o movimento adverso de uma largura de banda completa desde o primeiro enchimento:
   - para posições compradas, se a mínima da vela estiver pelo menos uma largura de banda abaixo do preço âncora, compre outra fatia cujo tamanho seja multiplicado
por `Multiplier` a cada novo nível;
   - para vendas, se a máxima da vela estiver uma largura de banda acima do preço âncora, venda uma fatia adicional usando o mesmo
lógica multiplicadora.
6. Continue agregando novos pedidos até que a meta de lucro ou perda máxima tolerada seja atingida.

## Gestão de dinheiro e saídas
- O auxiliar `CalculateUnrealizedProfit` reproduz o cálculo do PnL flutuante MT4 convertendo diferenças de preço em preço
etapas (`Security.PriceStep`) e valor da etapa (`Security.StepPrice`).
- A proxy de capital investido usa a fórmula original `Lots * Price / TickSize * TickValue / 30`, onde `Lots` se torna a soma
dos volumes da grade e os parâmetros de tick são provenientes de `Security`.
- Feche a cesta inteira quando o lucro flutuante exceder `TakeProfitFactor * invested capital`.
- Forçar uma liquidação de emergência quando a perda flutuante atingir `10 * TakeProfitFactor * invested capital` (mesmo índice do
Código MT4).
- Todas as saídas são executadas com ordens de mercado na direção oposta; uma vez plana, o estado da grade é redefinido e novos toques devem ser
detectado antes que outra entrada possa ser acionada.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `TakeProfitFactor` | `decimal` | `0.05` | Multiplicador aplicado ao capital investido para definir a meta de lucro. |
| `Multiplier` | `decimal` | `1.5` | Multiplicador de volume para cada pedido médio adicional. |
| `BollingerPeriod` | `int` | `20` | Número de velas usadas pelo indicador Bollinger Bandas. |
| `InitialVolume` | `decimal` | `1` | Volume base da primeira negociação em uma nova cesta (arredondado para os limites do local). |
| `CandleType` | `DataType` | Período de 15 minutos | Prazo usado para geração de sinal. |

## Diferenças do original EA
- StockSharp trabalha com posições líquidas; a estratégia mantém listas explícitas de entradas de grade para emular a ordem baseada em tickets do MT4
manuseio.
- Os filtros de volume do Exchange (`Security.MinVolume`, `Security.VolumeStep`, `Security.MaxVolume`) são aplicados automaticamente
de chamar manualmente `CheckVolumeValue`.
- Os sinais são avaliados em velas fechadas; os gatilhos intrabarra da versão MT4 são aproximados usando máximos e mínimos de velas
para decisões de média.
- As ordens são sempre enviadas como instruções de mercado, enquanto o MT4 usou `OrderSend` com parâmetros de compra/venda explícitos.

## Notas de uso
- Fornece metadados realistas para o instrumento negociado: `PriceStep`, `StepPrice`, `MinVolume`, `VolumeStep` e `MaxVolume` devem
ser preenchido para os cálculos de lucro, perda e volume para corresponder ao comportamento do MT4.
- Como a grade média é dimensionada geometricamente, teste a configuração em dados históricos e considere a margem do corretor
requisitos antes de executá-lo ao vivo.
- A largura da grade é igual à largura de banda atual Bollinger; alterar `BollingerPeriod` afeta diretamente o tempo de entrada e a grade
espaçamento. Valide a sensibilidade durante a otimização.
