# Estratégia Especialista RSI Stochastic MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Especialista RSI Stochastic MA** é uma conversão do consultor especialista MetaTrader 5 `Expert_RSI_Stochastic_MA.mq5`. A implementação em C# aproveita a API de estratégias de alto nível do StockSharp enquanto reproduz a lógica original: um filtro de tendência baseado em uma média móvel configurável, confirmação de momentum do RSI, e um oscilador Stochastic de linha dupla para timing preciso. O comportamento de proteção replica o algoritmo fonte com um limite de perda fixo opcional e uma saída trailing orientada por Stochastic.

## Indicadores e Parâmetros
A estratégia expõe as mesmas entradas que a versão MetaTrader e mantém seus valores padrão. Todos os parâmetros estão disponíveis para otimização através da UI do StockSharp.

| Categoria | Parâmetro | Padrão | Descrição |
| --- | --- | --- | --- |
| Geral | `CandleType` | Período de 15 minutos | Agregação de velas usada para cálculos de indicadores. |
| Trading | `TradeVolume` | `0.01` | Tamanho base do pedido em lotes/contratos. |
| RSI | `RsiPeriod` | `3` | Número de barras usadas para calcular o RSI. |
| RSI | `RsiPriceType` | Fechamento | Preço aplicado para RSI (fechamento, abertura, máximo, mínimo, mediano, típico, ponderado). |
| RSI | `RsiUpperLevel` | `80` | Limite de sobrecompra que aciona condições vendidas. |
| RSI | `RsiLowerLevel` | `20` | Limite de sobrevenda que aciona condições compradas. |
| Stochastic | `StochKPeriod` | `6` | Período da linha %K. |
| Stochastic | `StochDPeriod` | `3` | Período da linha de suavização %D. |
| Stochastic | `StochSlowing` | `3` | Fator de desaceleração adicional aplicado a %K. |
| Stochastic | `StochUpperLevel` | `70` | Nível de sobrecompra compartilhado por ambas as linhas Stochastic. |
| Stochastic | `StochLowerLevel` | `30` | Nível de sobrevenda compartilhado por ambas as linhas Stochastic. |
| Média Móvel | `MaMethod` | Simples | Tipo de média móvel (simples, exponencial, suavizada, ponderada). |
| Média Móvel | `MaPriceType` | Fechamento | Preço aplicado para a média móvel. |
| Média Móvel | `MaPeriod` | `150` | Comprimento da média móvel. |
| Média Móvel | `MaShift` | `0` | Número de barras completadas usadas para deslocar o valor da média móvel para trás. |
| Risco | `AllowLossPoints` | `30` | Máxima excursão adversa em pontos antes de sair de uma operação perdedora (0 desabilita). |
| Risco | `TrailingStopPoints` | `30` | Distância em pontos para o stop trailing baseado em Stochastic (0 fecha no Stochastic sem trailing). |

> **Cálculo de pontos** – A implementação converte os parâmetros `AllowLoss` e `TrailingStop` em preços absolutos usando `Security.PriceStep`. Quando o instrumento tem 3 ou 5 casas decimais, o valor é multiplicado por 10 para emular o tratamento de pips do MetaTrader.

## Lógica de Trading
### Configuração Comprado
1. **Filtro de tendência** – O fechamento da vela deve permanecer acima da média móvel deslocada.
2. **Confirmação de momentum** – RSI deve estar abaixo de `RsiLowerLevel`.
3. **Timing** – Ambas as linhas Stochastic (%K e %D) devem estar abaixo de `StochLowerLevel`.
4. **Filtro de posição** – Ordens compradas só são colocadas quando não existe exposição comprada (`Position <= 0`). O tamanho do pedido é `TradeVolume` mais qualquer quantidade necessária para fechar uma posição vendida existente.

### Configuração Vendido
1. **Filtro de tendência** – O fechamento da vela deve estar abaixo da média móvel deslocada.
2. **Confirmação de momentum** – RSI deve exceder `RsiUpperLevel`.
3. **Timing** – Ambas as linhas Stochastic devem estar acima de `StochUpperLevel`.
4. **Filtro de posição** – Novas posições vendidas requerem `Position >= 0`. A estratégia compensa comprados existentes automaticamente se necessário.

### Gestão de Saídas
- **Operações perdedoras**
  - Quando `AllowLossPoints` é zero, a estratégia aguarda que a linha principal do Stochastic se mova para o extremo oposto (`StochUpperLevel` para comprados, `StochLowerLevel` para vendidos) antes de fechar operações negativas.
  - Quando `AllowLossPoints` é positivo, a estratégia converte o valor em um deslocamento de preço e fecha a operação assim que a perda exceder este limite *e* o Stochastic retornar à zona neutra (`stochMain > StochLowerLevel` para comprados, `< StochUpperLevel` para vendidos).
- **Saída trailing**
  - Com `TrailingStopPoints > 0`, uma vez que uma operação é lucrativa e o Stochastic atinge sua zona extrema, um stop trailing é definido em cada vela finalizada. Para operações compradas, o stop segue abaixo do preço; para vendidas, segue acima.
  - Com `TrailingStopPoints = 0`, operações lucrativas são fechadas imediatamente quando o Stochastic atinge o nível extremo (correspondendo ao comportamento do EA original).
- **Gatilho de trailing** – As atualizações de trailing ocorrem apenas em velas completadas, espelhando a implementação MQL que restringia atualizações a uma por barra.

## Notas de Implementação
- O deslocamento da média móvel é tratado armazenando valores recentes e lendo o valor `MaShift` barras atrás, reproduzindo o parâmetro `shift` do MetaTrader.
- As entradas de RSI e média móvel suportam múltiplos preços aplicados para corresponder às opções do MetaTrader. Os cálculos do Stochastic dependem do oscilador integrado do StockSharp (modo Low/High) e respeitam os comprimentos de suavização configurados.
- Os limites de trailing e perda são medidos em *pontos*. O auxiliar escala automaticamente o valor para tamanhos de tick típicos de FX (3 ou 5 decimais) e por padrão usa um `PriceStep`.
- A saída do gráfico inclui velas, a média móvel, RSI e indicadores Stochastic, permitindo validação visual semelhante ao modelo original.
- Não há versão Python adjunta por solicitação; apenas a implementação em C# é fornecida.

## Dicas de Uso
- Ao implantar em ativos com tamanhos de tick não convencionais, verifique se `Security.PriceStep` está preenchido; caso contrário, a conversão padrão será usada (1 ponto = 1 unidade de preço).
- Combine o `StartProtection` integrado ou módulos de risco adicionais se for necessário mais gerenciamento de stop-loss ou take-profit.
- Otimize os comprimentos dos indicadores e os limites de risco juntos — a estratégia expõe intencionalmente todos os controles primários do especialista MetaTrader.
