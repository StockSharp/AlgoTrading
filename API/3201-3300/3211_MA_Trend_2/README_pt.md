# Estratégia de MA Trend 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
- Convertida do consultor especialista MetaTrader 5 `MA Trend 2.mq5`.
- Usa uma média móvel configurável para detectar se o preço opera acima ou abaixo da média deslocada.
- As posições são gerenciadas com stop-loss, take-profit, trailing stop e recursos de gerenciamento de dinheiro opcionais.

## Lógica da estratégia
1. Subscrever à série de velas selecionada pelo usuário e calcular a média móvel com o método, período, deslocamento e fonte de preço escolhidos.
2. Em cada vela concluída, armazenar o último valor da média móvel para que uma amostra deslocada (barra anterior mais `MaShift`) possa ser comparada com o preço de fechamento atual.
3. Gerar sinais de compra quando o preço cruza acima da média de referência e o filtro de direção permite negociações longas. Gerar sinais de venda para a condição oposta. Quando `ReverseSignals` está habilitado, essas regras são invertidas.
4. Antes de entrar em uma negociação, verificar os sinalizadores `OnlyOnePosition` e `CloseOppositePositions`. A estratégia pode pular entradas quando a exposição oposta existe ou fechá-la na mesma ordem para inverter a posição.
5. O dimensionamento de posição usa um volume fixo ou um modelo de percentual de risco derivado do EA original. O modo percentual estima o volume necessário para que a perda na distância de stop configurada corresponda ao orçamento de risco.
6. Um trailing stop replica a lógica de passos original: uma vez que o lucro excede `TrailingStopPips + TrailingStepPips`, move o stop em passos sem nunca afrouxá-lo. Se o preço cruzar o trailing stop, a posição é fechada a mercado.
7. Proteções opcionais de stop-loss e take-profit são anexadas através do auxiliar de alto nível `StartProtection` para que o modelo de corretor possa fechar posições entre atualizações de velas.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `StopLossPips` | Distância do stop-loss em pips. Definir como `0` para desabilitar. | `50` |
| `TakeProfitPips` | Distância do take-profit em pips. Definir como `0` para desabilitar. | `140` |
| `TrailingStopPips` | Distância base para o trailing stop em pips. | `15` |
| `TrailingStepPips` | Lucro mínimo adicional antes do trailing stop ser ajustado. | `5` |
| `LotMode` | `FixedVolume` usa `LotOrRiskValue` diretamente. `RiskPercent` interpreta como percentual de risco da conta. | `RiskPercent` |
| `LotOrRiskValue` | Tamanho de ordem fixo ou percentual de risco dependendo de `LotMode`. | `3` |
| `MaPeriod` | Período da média móvel. | `12` |
| `MaShift` | Número de velas concluídas entre a barra atual e a amostra de média móvel usada para sinais. | `3` |
| `MaMethod` | Método de média móvel (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `LinearWeighted` |
| `MaPrice` | Preço de vela utilizado pela média móvel (fechamento, abertura, ponderado, etc.). | `Weighted` |
| `CandleType` | Tipo de dados de vela subscrito pela estratégia. | `1 minute time frame` |
| `Direction` | Direção permitida (`BuyOnly`, `SellOnly`, `Both`). | `Both` |
| `OnlyOnePosition` | Permitir apenas uma única posição aberta. | `false` |
| `ReverseSignals` | Inverter a lógica de compra/venda. | `false` |
| `CloseOppositePositions` | Fechar exposição oposta antes de abrir uma nova negociação. | `false` |

## Gerenciamento de dinheiro
- Quando `LotMode = RiskPercent`, a estratégia converte a distância do stop-loss (em pips) em unidades de preço usando metadados do ativo (`PriceStep`, `StepPrice`).
- O risco é calculado a partir do valor do portfólio (`CurrentValue` com fallback para `BeginValue`).
- O volume solicitado é arredondado para cima para o `VolumeStep` mais próximo para evitar rejeições da bolsa.

## Trailing stop
- A distância e o passo do trailing são expressos em pips; o código deriva a distância de preço real usando o tamanho de pip do instrumento.
- Posições longas movem o stop para cima quando o fechamento excede a entrada por pelo menos `TrailingStopPips + TrailingStepPips`. O stop permanece fixo se o lucro recuar.
- Posições curtas espelham a mesma lógica com verificações de preço simétricas.

## Notas de conversão
- Todas as ações de negociação usam a API de alto nível de `Strategy` (`BuyMarket`, `SellMarket`, `StartProtection`).
- A estratégia mantém apenas um histórico curto de média móvel (deslocamento + buffer) para replicar a referência de barra anterior sem armazenar grandes conjuntos de dados.
- Comentários são fornecidos em inglês para documentar cada bloco principal de lógica.
