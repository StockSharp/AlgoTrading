# Estratégia especializada em Martingail
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Martingail Expert é uma estratégia de martingale que segue tendências e depende do oscilador Stochastic para cronometrar novas sequências de negociações. Uma vez que o indicador gera uma direção, a estratégia inicia uma escada de ordens de mercado e gerencia a exposição usando uma meta de lucro dinâmica e um esquema geométrico de dimensionamento de posição.

## Lógica de negociação
- Calcule um oscilador Stochastic na série de velas configurada. Os valores finais mais recentes de %K e %D são armazenados em cache para tomada de decisão.
- Inicie uma nova sequência longa quando `%K (previous) > %D (previous)` e `%D (previous)` estiverem acima do limite `BuyLevel`.
- Inicie uma nova sequência curta quando `%K (previous) < %D (previous)` e `%D (previous)` estiverem abaixo do limite `SellLevel`.
- Depois de entrar em uma sequência, cada movimento de preço favorável igual a `ProfitFactor × openOrders` pips adiciona uma nova posição ao volume base.
- Cada movimento adverso de `StepPoints` pips multiplica o último volume preenchido por `Multiplier` e envia uma ordem média na mesma direção.

## Regras de saída
- Feche toda a posição assim que o último preço de preenchimento atingir uma meta de lucro dinâmico de `ProfitFactor × openOrders` pips na direção favorável.
- Redefina o estado martingale sempre que o tamanho da posição agregada retornar a zero.

## Gestão de risco
A progressão martingale aumenta a exposição rapidamente quando o preço se move contra a posição. Ajuste `Multiplier`, `StepPoints` e `ProfitFactor` cuidadosamente para corresponder ao tamanho da conta e à volatilidade do instrumento.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `Volume` | Volume base de ordem de mercado usado para a primeira negociação e todos os complementos favoráveis. |
| `Multiplier` | Fator aplicado ao último volume executado ao calcular a média durante movimentos adversos. |
| `StepPoints` | Distância em pontos que aciona uma ordem média de martingale. |
| `ProfitFactor` | Meta de lucro por pedido aberto expressa em pontos. A distância real é `ProfitFactor × number_of_orders`. |
| `KPeriod` | Comprimento de lookback para a linha %K. |
| `DPeriod` | Suavização do comprimento da linha %D. |
| `Slowing` | Suavização adicional aplicada a %K antes de comparar com %D. |
| `BuyLevel` | Valor mínimo de %D necessário para permitir uma nova sequência longa. |
| `SellLevel` | Valor máximo de %D necessário para permitir uma nova sequência curta. |
| `CandleType` | Série de velas usada para cálculos (padrão: período de 5 minutos). |

## Notas
- Funciona melhor em pares de FX líquidos onde o tamanho do pip e a etapa de volume permitem escala granular.
- Requer margem suficiente para suportar vários passos de martingale.
