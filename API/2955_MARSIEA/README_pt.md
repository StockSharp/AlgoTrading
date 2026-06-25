# Estratégia MA RSI EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia MA RSI EA** reproduz a lógica do assessor especializado original do MetaTrader que combina uma média móvel rápida com um filtro RSI de período curto. A estratégia opera na série de velas selecionada, avalia novas ordens apenas em barras terminadas e usa dimensionamento de posição dinâmico baseado no saldo ou patrimônio da conta. Quando o lucro flutuante de todas as posições abertas se torna positivo, cada posição é fechada imediatamente para assegurar o ganho.

## Indicadores
- **Moving Average** – método configurável (simples, exponencial, suavizado, ponderado linearmente) com seleção de fonte de preço e deslocamento opcional.
- **Relative Strength Index (RSI)** – oscilador de curto prazo que lê da mesma família de preços de velas da versão MQL.

## Lógica de trading
1. Para cada vela concluída, a estratégia calcula os valores de média móvel e RSI usando as fontes de preço configuradas.
2. O valor de média móvel mais recente pode ser deslocado por um número de barras definido pelo usuário para corresponder ao comportamento MQL.
3. Ela avalia o PnL flutuante da posição líquida atual:
   - Se o resultado flutuante de todas as posições abertas for **maior que zero**, a estratégia fecha a posição inteira para realizar o lucro.
   - Se o resultado flutuante for **negativo**, o lado com a menor perda (lado comprador vs. lado vendedor) é reforçado abrindo uma operação adicional nessa direção.
4. Se não houver sinal de média, o filtro RSI + MA é aplicado:
   - **Entrada vendido** – RSI ≥ `RsiOverbought` e o preço de abertura da vela está abaixo da média móvel deslocada.
   - **Entrada comprado** – RSI ≤ `RsiOversold` e o preço de abertura da vela está acima da média móvel deslocada.

## Lógica de saída
- PnL flutuante positivo aciona `CloseAllPositions`, zerando a estratégia imediatamente.
- Sinais de reversão manual da lógica de média fecham a exposição oposta porque o StockSharp trabalha com posições líquidas.

## Dimensionamento de posição
`LotSizingModes` espelha a seleção `OptLot` do EA:
- **Fixed** – sempre envia volume `LotSize`.
- **Balance** – converte `PercentOfBalance` do valor da carteira em volume usando o preço de fechamento da vela.
- **Equity** – converte `PercentOfEquity` do patrimônio atual da carteira em volume.

O volume calculado é arredondado para o `Security.VolumeStep` mais próximo (quando disponível) para que as ordens cumpram com o tamanho de lote do instrumento.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `LotOption` | Modo de cálculo de volume (`Fixed`, `Balance`, `Equity`). | `Balance` |
| `LotSize` | Valor de lote fixo para o modo `Fixed`. | `0.01` |
| `PercentOfBalance` | Porcentagem do saldo usada no modo `Balance`. | `2` |
| `PercentOfEquity` | Porcentagem do patrimônio usada no modo `Equity`. | `3` |
| `FastMaPeriod` | Comprimento da média móvel. | `4` |
| `FastMaShift` | Deslocamento aplicado ao resultado da média móvel. | `0` |
| `FastMaMethod` | Método de cálculo da média móvel (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `LinearWeighted` |
| `FastMaPrice` | Fonte de preço de vela para a média móvel. | `Open` |
| `RsiPeriod` | Comprimento do RSI. | `4` |
| `RsiPrice` | Fonte de preço de vela para o RSI. | `Open` |
| `RsiOverbought` | Nível RSI que define um mercado sobrecomprado. | `80` |
| `RsiOversold` | Nível RSI que define um mercado sobrevendido. | `20` |
| `CandleType` | Série de velas usada pela estratégia. | `Período de 15 minutos` |

## Fontes de preço de vela
`CandlePriceSources` replica a lista de preços aplicados MQL:
- `Open`, `High`, `Low`, `Close`
- `Median` = (High + Low) / 2
- `Typical` = (High + Low + Close) / 3
- `Weighted` = (High + Low + Close + Close) / 4

## Notas
- As ordens são geradas apenas quando a estratégia está online e a vela terminou, correspondendo ao EA original que aciona em novas barras.
- Como o StockSharp mantém uma posição líquida, os sinais de média automaticamente reduzem ou invertem a exposição atual em vez de criar posições de hedge.
- A implementação em Python é intencionalmente omitida conforme solicitado.
