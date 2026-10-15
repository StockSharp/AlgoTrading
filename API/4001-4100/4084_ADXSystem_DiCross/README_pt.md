# ADX Estratégia cruzada de DI do sistema
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia do sistema ADX é a StockSharp conversão do MetaTrader 4 especialista `ADX_System.mq4`. O EA original compara o
Índice Direcional Médio (ADX) com seus componentes +DI e -DI nas duas velas concluídas mais recentes. Quando a linha +DI
sobe acima do valor ADX que o sistema deseja que seja longo; quando a linha -DI ultrapassa o valor ADX, ela deseja ser curta. O
A porta StockSharp reproduz esse comportamento armazenando os valores dos indicadores das duas velas concluídas anteriores para que a lógica
espelha as chamadas `iADX(..., shift=1/2)` usadas no código MetaTrader.

Apenas uma posição pode ser aberta por vez. A estratégia envia ordens de mercado para entradas e saídas, combinando o bilhete único
lógica de MetaTrader contas de compensação. A gestão de risco reflete o consultor especialista original: take-profit fixo e stop-loss
os níveis são expressos em pontos relativos ao preço médio de entrada, e um trailing stop opcional pode garantir lucros assim que o
posição se move favoravelmente.

## Lógica de negociação
1. Assine o prazo configurado (`CandleType`) e processe apenas velas finalizadas para evitar decisões intra-barras.
2. Alimente um indicador `AverageDirectionalIndex` com os dados da vela e espere até que o indicador forneça seu ADX, +DI e -DI
valores.
3. Armazene em cache as leituras do indicador das duas velas finalizadas mais recentes para que a estratégia possa fazer referência ao "atual" e
Valores "anteriores" exatamente como a implementação MetaTrader.
4. **Entrada longa**: se o ADX (`shift = 2`) mais antigo estiver abaixo do ADX (`shift = 1` mais recente), o +DI mais antigo estará abaixo daquele mais antigo
ADX, e o +DI mais recente estiver acima do mais recente ADX, envie uma ordem de compra a mercado.
5. **Entrada curta**: se as mesmas condições aparecerem para o componente -DI (antigo -DI abaixo do antigo ADX, novo -DI acima do novo ADX), envie um
ordem de venda no mercado.
6. **Saída longa**: feche a posição comprada quando o ADX começar a cair e +DI cruzar novamente abaixo dele, quando o configurado
o take-profit ou stop-loss é atingido, ou quando o trailing stop é violado.
7. **Saída curta**: espelha a lógica de saída longa usando -DI junto com os controles de risco.
8. Atualize o histórico do indicador em cache após cada vela para que o próximo sinal use o par `shift = 1/2` mais recente.

## Gestão de risco
- `TakeProfitPoints` e `StopLossPoints` descrevem distâncias em pontos no estilo MetaTrader. Eles são convertidos em unidades de preço reais
usando `Security.PriceStep` quando disponível; caso contrário, o valor bruto é tratado como um delta de preço absoluto.
- O trailing stop (`TrailingStopPoints`) é ativado somente após a posição ganhar pelo menos a distância configurada do
preço de entrada. Uma vez ativo, ele se move na direção do lucro e fecha a posição quando o preço cruza o nível final.
- Todas as saídas (reversão do indicador, take-profit, stop-loss, trailing stop) utilizam ordens de mercado para que a posição seja achatada
imediatamente, imitando o comportamento `OrderClose` da fonte EA.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 1 minuto | Período primário processado pela estratégia. |
| `AdxPeriod` | `int` | `14` | Número de velas usadas para calcular os componentes ADX e DI. |
| `TradeVolume` | `decimal` | `1` | Tamanho do lote usado para cada ordem de mercado. |
| `TakeProfitPoints` | `decimal` | `100` | Distância de take-profit em pontos em relação ao preço de entrada. |
| `StopLossPoints` | `decimal` | `30` | Distância de stop-loss em pontos em relação ao preço de entrada. |
| `TrailingStopPoints` | `decimal` | `0` | Distância de parada móvel opcional em pontos. Defina como zero para desativar o rastreamento. |

## Diferenças do especialista MetaTrader original
- MetaTrader gerencia tickets individuais enquanto StockSharp trabalha com uma única posição líquida. A conversão fecha, portanto, o
posição atual antes de emitir uma nova ordem de entrada quando o sinal mudar.
- O EA original dependia de `Point` para converter pontos em distâncias de preço. A porta StockSharp usa `Security.PriceStep` quando
é conhecido; caso contrário, a distância será tratada como unidades de preço bruto, portanto, poderá ser necessário ajustar os padrões para instrumentos com
etapas de preços não convencionais.
- MetaTrader aplica trailing stops modificando a ordem existente. StockSharp fecha a posição com uma ordem de mercado quando o
o trailing stop é violado, o que é funcionalmente equivalente, mas mais simples dentro do modelo de compensação.

## Dicas de uso
- Certifique-se de que o volume da estratégia (`TradeVolume`) esteja alinhado com a etapa do lote do instrumento. O construtor também atribui esse valor a
`Strategy.Volume`, então os métodos auxiliares usam o tamanho de negociação esperado.
- Aumente `TakeProfitPoints` e `StopLossPoints` se você negocia instrumentos com faixas médias maiores ou etapas de preços menores.
- Adicione a estratégia a um gráfico para visualizar as velas, o indicador ADX e as negociações executadas, o que ajuda a verificar se os sinais
ocorrem exatamente quando +DI ou -DI cruza acima da linha ADX.

## Indicadores
- `AverageDirectionalIndex` (fornece ADX junto com componentes +DI e -DI).
