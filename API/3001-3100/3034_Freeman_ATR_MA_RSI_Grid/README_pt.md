# Estratégia Freeman ATR MA RSI Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o expert advisor MetaTrader "Freeman" usando a API de alto nível do StockSharp. Acumula múltiplas posições de mercado enquanto uma tendência medida pela inclinação de uma média móvel permanece alinhada com uma confirmação RSI. Cada distância de entrada e saída é definida em pips e convertida em unidades de preço usando o tamanho do tick do instrumento para que o comportamento corresponda à implementação forex original.

## Lógica de negociação
1. Assinar uma única série de candles (período configurável) e atualizar os indicadores ATR, média móvel e RSI em cada candle finalizado.
2. Gerar um sinal direcional quando:
   - A inclinação da média móvel é positiva ou negativa comparando o valor mais recente com a barra anterior (filtro de tendência opcional).
   - O preço está suficientemente distante da média móvel para evitar entradas diretamente na linha.
   - O RSI cruza o limiar superior ou inferior se o filtro RSI estiver habilitado. A lógica do MetaTrader é mantida intacta, incluindo a peculiaridade onde uma confirmação de venda RSI retorna `-11`, portanto, ativar ambos os filtros favorece apenas operações compradas.
3. Respeitar o número máximo de posições abertas simultaneamente. Entradas adicionais na mesma direção são permitidas somente quando o preço se moveu contra o último preenchimento pela distância de pip configurada, construindo efetivamente uma grade.
4. Cada entrada usa níveis de stop-loss e take-profit baseados em ATR. Os trailing stops ajustam o stop protetor uma vez que o preço se move pelo passo de trailing mais a distância do trailing stop.
5. As saídas são executadas via ordens de mercado opostas quando o intervalo do candle atinge o nível de stop, alvo ou trailing.

## Gestão de risco
- Os multiplicadores ATR controlam as distâncias fixas de stop-loss e take-profit. Definir um multiplicador para zero desabilita essa proteção.
- Os trailing stops são opcionais e são definidos por dois parâmetros de pip: a distância de trailing real e o passo adicional necessário antes de mover o stop novamente.
- A estratégia depende da propriedade base `Volume` para dimensionamento; nenhum gerenciamento monetário automatizado é aplicado além do limite de posição.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período usado para cálculos de indicadores. |
| `MaxPositions` | Número máximo de posições abertas simultaneamente (soma de compradas e vendidas). |
| `DistancePips` | Distância mínima em pips entre entradas consecutivas na mesma direção. |
| `AtrPeriod` | Período de média para o indicador ATR. |
| `AtrStopLossMultiplier` | Multiplicador ATR para o stop protetor. `0` desabilita o stop. |
| `AtrTakeProfitMultiplier` | Multiplicador ATR para o alvo de lucro. `0` desabilita o alvo. |
| `UseTrendFilter` | Habilita o filtro de inclinação da média móvel. |
| `DistanceFromMaPips` | Distância mínima em pips entre preço e a média móvel quando o filtro de tendência está ativo. |
| `MaPeriod`, `MaShift`, `MaMethod`, `MaPriceType` | Parâmetros de média móvel espelhando as entradas do MetaTrader. |
| `UseRsiFilter` | Habilita o filtro de confirmação RSI. |
| `RsiLevelUp`, `RsiLevelDown`, `RsiPeriod`, `RsiPriceType` | Configuração RSI com seleção de preço aplicado. |
| `TrailingStopPips`, `TrailingStepPips` | Distância e passo do trailing stop medidos em pips. |
| `CurrentBarOffset` | Deslocamento aplicado ao ler valores do indicador, emulando a entrada `CurrentBar` do expert advisor. |

## Notas
- A conversão de pips multiplica o `PriceStep` do instrumento por 10 quando o instrumento tem 3 ou 5 casas decimais para reproduzir o ajuste ponto-para-pip do MetaTrader.
- A estratégia usa um modelo de posição de compensação; sinais opostos fecham posições existentes antes de abrir operações na nova direção.
- A proteção de início é habilitada no lançamento para proteger contra reconexões inesperadas antes que quaisquer operações sejam colocadas.
