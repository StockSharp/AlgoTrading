# Estratégia VR ZVER
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A estratégia VR ZVER é um sistema de seguidor de tendência que combina três camadas de confirmação: uma pilha de EMA rápida/lenta/muito lenta, o Oscilador Estocástico e o Índice de Força Relativa (RSI). Todos os filtros ativos devem concordar antes que uma posição seja aberta, o que ajuda a evitar trades durante regimes de mercado turbulentos e contraditórios. A conversão mantém a lógica original de break-even e proteção enquanto usa a API de alto nível do StockSharp.

## Detecção do Regime de Mercado
1. **Estrutura EMA** – A configuração padrão usa médias móveis exponenciais com períodos 3, 5 e 7. Um viés comprado requer que a EMA rápida esteja acima da EMA lenta e que a EMA lenta permaneça acima da EMA muito lenta. Um viés vendido inverte essa relação.
2. **Oscilador Estocástico** – O par %K/%D é inspecionado tanto para direção quanto para nível. Os trades comprados requerem que %K esteja abaixo da banda inferior e acima de %D, sinalizando um rebote de sobrevenda. Os trades vendidos requerem que %K esteja acima da banda superior e abaixo de %D, apontando para uma reversão de sobrecompra.
3. **Filtro RSI** – O RSI deve estar abaixo do limiar inferior para permitir entradas compradas ou acima do limiar superior para habilitar trades vendidos.

Somente quando cada filtro habilitado está alinhado a estratégia envia uma ordem a mercado usando o volume configurado.

## Gestão de Risco
- **Stop Loss** – Cada entrada projeta um stop baseado em preço usando a configuração `StopLossPips` multiplicada pelo tamanho de pip do instrumento. As posições compradas saem quando a mínima do candle perfura o stop, enquanto as posições vendidas fecham se a máxima do candle atingir seu stop.
- **Take Profit** – Um nível de take-profit simétrico é aplicado. Se o candle atual atingir o alvo a favor da operação, a posição é fechada imediatamente.
- **Proteção de Break-Even** – Após o preço avançar a distância `BreakevenPips`, um modo de break-even é armado. Qualquer retração de volta ao preço de entrada irá nivelar a posição para preservar o capital.
- **Limpeza de Ordens** – Todas as ordens ativas são canceladas antes de abrir um novo trade para evitar empilhamento não intencional.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `CandleType` | Série de velas usada para cálculos. |
| `UseMovingAverage` | Habilita ou desabilita o filtro de tendência EMA. |
| `FastMaPeriod`, `SlowMaPeriod`, `VerySlowMaPeriod` | Períodos para as EMAs rápida, lenta e muito lenta. |
| `UseStochastic` | Alterna a camada de confirmação Estocástica. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Configurações de período para o Oscilador Estocástico. |
| `StochasticUpperLevel`, `StochasticLowerLevel` | Limiares de sobrecompra e sobrevenda para %K. |
| `UseRsi` | Habilita ou desabilita a camada de confirmação RSI. |
| `RsiPeriod` | Período de médio RSI. |
| `RsiUpperLevel`, `RsiLowerLevel` | Limiares RSI que definem regiões de sobrecompra/sobrevenda. |
| `StopLossPips`, `TakeProfitPips` | Distâncias (em pips) para colocação de stop-loss e take-profit. |
| `BreakevenPips` | Progresso de preço necessário antes de ativar a proteção de break-even. |
| `Volume` | Quantidade a negociar para cada ordem a mercado. |

## Notas de Implementação
- O tamanho de pip é derivado do passo de preço do instrumento e do número de casas decimais. Instrumentos com 3 ou 5 casas decimais aplicam automaticamente o ajuste padrão de 10x usado na versão MQL original.
- Todos os dados do indicador são acessados através de `BindEx`, garantindo que a estratégia reaja apenas a velas completas com valores de indicador finalizados.
- A estratégia é plana por padrão; as posições nunca são invertidas sem fechar primeiro a exposição existente.
