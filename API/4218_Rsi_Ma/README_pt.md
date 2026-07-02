# 4218 RSI Estratégia MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma porta C# do consultor especialista MetaTrader original localizado em `MQL/9925`. Ele recria o oscilador de momento RSI_MA combinando um clássico RSI com a inclinação de uma média móvel exponencial construída no preço ponderado `(High + Low + 2 * Close) / 4`. Os sinais são gerados apenas em velas concluídas, mantendo o comportamento idêntico ao da implementação de origem.

O script foi projetado para velas EURUSD diárias (período D1) e abre uma única posição por vez. No entanto, qualquer instrumento com uma variação de preço significativa pode ser usado, desde que o tipo de vela esteja configurado adequadamente.

## Lógica estratégica
1. **Cálculo do indicador**
   - Um Índice de Força Relativa com comprimento configurável é calculado nos preços de fechamento.
   - Uma média móvel exponencial com o mesmo comprimento é calculada sobre o preço ponderado.
   - O valor do indicador é igual a `RSI * (EMA(current) - EMA(previous)) / pipSize` e é cortado no intervalo `[1, 99]`.
2. **Entrada longa**
   - Valor anterior do indicador abaixo do extremo de sobrevenda (padrão 5).
   - Último valor do indicador acima do limite de ativação de sobrevenda (padrão 20).
   - Nenhuma posição aberta ou uma posição curta existente (a venda é fechada antes de abrir uma nova compra).
3. **Entrada curta**
   - Valor do indicador anterior acima do extremo de sobrecompra (padrão 95).
   - Último valor do indicador abaixo do limite de ativação de sobrecompra (padrão 80).
   - Nenhuma posição aberta ou uma posição longa existente (a compra é fechada antes de abrir uma nova venda).
4. **Saída baseada em indicador**
   - As posições longas são fechadas quando o indicador cai acima do extremo de sobrecompra para abaixo do nível de ativação (95 → 80 por padrão).
   - As posições curtas são fechadas quando o indicador sobe abaixo do extremo de sobrevenda para acima do nível de ativação (5 → 20 por padrão).
5. **Saídas de proteção**
   - As distâncias opcionais de stop-loss, take-profit e trailing stop são expressas em pips. As distâncias são automaticamente convertidas em preço usando o título `PriceStep` (fallback 0,0001).
   - O aperto do trailing stop segue o comportamento do EA original: ele é ativado somente depois que o preço se move mais do que a distância configurada na direção favorável.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `RsiPeriod` | Comprimento RSI e EMA.|
| `OversoldActivationLevel` | Limite que confirma uma configuração longa após um extremo de sobrevenda. |
| `OversoldExtremeLevel` | Extremo que deve ser alcançado antes que os longos sejam permitidos. |
| `OverboughtActivationLevel` | Limite que confirma uma configuração curta após um extremo de sobrecompra. |
| `OverboughtExtremeLevel` | Extremo que deve ser alcançado antes que os shorts sejam permitidos. |
| `StopLossPips` | Distância para o stop loss de proteção. Ativar/desativar via `UseStopLoss`. |
| `TakeProfitPips` | Distância para a meta de lucro. Ativar/desativar via `UseTakeProfit`. |
| `TrailingStopPips` | Distância para o trailing stop. Ativar/desativar via `UseTrailingStop`. |
| `UseStopLoss` | Ativa o gerenciamento de stop-loss. |
| `UseTakeProfit` | Ativa o gerenciamento de take-profit. |
| `UseTrailingStop` | Ativa atualizações de trailing stop. |
| `UseMoneyManagement` | Ativa o dimensionamento da posição com base em `RiskPercent`. |
| `RiskPercent` | Percentagem da carteira arriscada por negociação quando a gestão de dinheiro está ativa. |
| `TradeVolume` | Volume fixo usado quando o gerenciamento de dinheiro está desabilitado. |
| `CandleType` | Tipo de dados das velas processadas pela estratégia (padrão Diário). |

## Notas de uso
- Anexe a estratégia às velas diárias do EURUSD para reproduzir o comportamento do EA original. Outros instrumentos/prazos são suportados após o ajuste de `CandleType` e limites.
- Apenas uma posição é mantida aberta por vez. Entrar em uma nova negociação fecha automaticamente a direção oposta primeiro.
- A gestão do dinheiro volta ao `TradeVolume` fixo sempre que as informações do portfólio não estão disponíveis ou o volume calculado se torna não positivo.
- Certifique-se de que a segurança `PriceStep` reflita um pip (0,0001 para a maioria dos pares FX). Caso contrário, ajuste os parâmetros adequadamente.

## Gestão de risco
- Os níveis de stop-loss e take-profit são avaliados em cada vela concluída usando os intervalos máximo/mínimo da vela.
- O trailing stop é atualizado somente quando a negociação lucra mais do que a distância configurada e nunca se move em uma direção desfavorável.
- As saídas baseadas em indicadores ainda funcionam mesmo quando os controles de risco estão desativados, garantindo uma degradação suave semelhante à versão MQL.
