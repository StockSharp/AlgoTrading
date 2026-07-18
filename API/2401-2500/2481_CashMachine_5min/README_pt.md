# Estratégia CashMachine de 5 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão direta do Expert Advisor **CashMachine 5min** de MQL para a API de alto nível do StockSharp. É projetada para velas de cinco minutos e combina o indicador DeMarker com um filtro de cruzamento do oscilador Estocástico. O gerenciamento de operações usa níveis ocultos de stop-loss / take-profit junto com regras de trailing por estágios que tentam travar ganhos assim que o momentum do preço aparece.

## Lógica de negociação
### Condições de entrada
- **Comprado**: Valor anterior do DeMarker abaixo de 0.30 e valor atual em ou acima de 0.30 **e** o Estocástico %K cruza acima de 20 na mesma vela. Nenhum posição deve estar aberta.
- **Vendido**: Valor anterior do DeMarker acima de 0.70 e valor atual em ou abaixo de 0.70 **e** o Estocástico %K cruza abaixo de 80. Nenhuma posição deve estar aberta.

### Gerenciamento de posição
- Apenas uma posição é mantida por vez; sinais opostos são ignorados até que a operação atual seja fechada.
- Saídas ocultas fecham a posição quando o preço toca `Entry ± HiddenStopLoss` ou `Entry ± HiddenTakeProfit` (valores interpretados em pips).
- Três alvos de lucro intermediários (`TargetTp1/2/3`) movem um trailing stop oculto para `preço atual - (alvo - 13)` pips para comprados e `preço atual + (alvo + 13)` pips para vendidos. Os 13 pips extras imitam o comportamento do EA original, travando lucros após cada marco sem sair imediatamente.
- Se o trailing stop for tocado após a ativação, a posição é fechada a mercado.

## Indicadores
- **DeMarker** – Detecta reversões de momentum; o parâmetro de comprimento corresponde ao período de média original.
- **Oscilador Estocástico** – Usa o período original de %K (`StochasticLength`), suavização de %K (`StochasticK`) e suavização de %D (`StochasticD`).

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `HiddenTakeProfit` | Distância oculta do take-profit em pips. | 60 |
| `HiddenStopLoss` | Distância oculta do stop-loss em pips. | 30 |
| `TargetTp1` | Primeiro nível de ativação do trailing (pips). | 20 |
| `TargetTp2` | Segundo nível de ativação do trailing (pips). | 35 |
| `TargetTp3` | Terceiro nível de ativação do trailing (pips). | 50 |
| `DeMarkerLength` | Período de média do DeMarker. | 14 |
| `StochasticLength` | Período de lookback do %K do Estocástico. | 5 |
| `StochasticK` | Comprimento de suavização do %K. | 3 |
| `StochasticD` | Comprimento de suavização do %D. | 3 |
| `CandleType` | Série de velas usada para cálculos (padrão 5 minutos). | Período de 5 minutos |

## Notas
- O tamanho do pip é derivado de `Security.PriceStep`. Quando o passo é desconhecido, é usado um valor de fallback de `0.0001`, reproduzindo a lógica do EA que ajusta para cotações de 3 e 5 dígitos.
- Todas as decisões de negociação são baseadas em velas terminadas; o comportamento intra-barra do EA original pode diferir ligeiramente porque a versão MQL rodava em cada tick.
- A estratégia depende do tratamento padrão de volume de ordens do StockSharp — definir `Strategy.Volume` para controlar o tamanho da operação.
