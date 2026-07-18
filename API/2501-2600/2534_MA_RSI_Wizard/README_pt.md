# Estratégia MA + RSI Wizard
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é o port do StockSharp do especialista do MetaTrader 5 "MQL5 Wizard MA RSI" da pasta `MQL/17489`. O robô original combina um filtro de média móvel com um filtro RSI e abre operações quando a soma ponderada dos filtros cruza limiares configuráveis. A versão em C# mantém a mesma estrutura expressando a lógica com a API de alto nível do StockSharp e auxiliares modernos de gestão de risco.

O bot funciona em qualquer instrumento que forneça velas OHLCV. Ele avalia uma média móvel que pode ser atrasada por um número de barras definido pelo usuário e um RSI que pode ser alimentado com diferentes fontes de preço. Ambos os indicadores contribuem para uma pontuação composta. Uma posição é aberta assim que a pontuação excede o limiar de abertura e fechada quando a pontuação contrária atinge o limiar de fechamento. As configurações opcionais de distância, stop loss e take profit replicam os parâmetros de gestão de dinheiro do Consultor Especialista original.

## Indicadores e pontuação

* **Média Móvel** – período configurável, método (simples, exponencial, suavizado, linearmente ponderado), fonte de preço e deslocamento para frente. Quando o preço de fechamento está acima da média deslocada, a pontuação MA é igual a 100, caso contrário é 0.
* **Índice de Força Relativa (RSI)** – período e fonte de preço configuráveis. A contribuição RSI cresce linearmente de 0 quando RSI = 50 para 100 quando RSI = 100 para sinais comprados, e espelha o mesmo comportamento para sinais vendidos.
* **Pontuação composta** – as pontuações MA e RSI são ponderadas por `MaWeight` e `RsiWeight`. O valor final é a média ponderada `score = (maScore * MaWeight + rsiScore * RsiWeight) / (MaWeight + RsiWeight)` que permanece dentro do intervalo [0;100] assim como na versão MetaTrader.
* **Filtro de distância de preço** – `PriceLevelPoints` define a distância mínima entre o fechamento da vela e a média móvel deslocada (convertida para preço usando o passo do instrumento). Sinais mais próximos que o limiar são ignorados.

## Regras de operação

1. Cada vela finalizada atualiza os indicadores e pontuações.
2. Se a pontuação oposta ultrapassar `ThresholdClose`, a posição atual é fechada a mercado.
3. Entrada comprada – permitida quando não há exposição comprada, a pontuação comprada é pelo menos `ThresholdOpen`, o tempo de espera (`ExpirationBars`) passou, e o filtro de distância de preço é satisfeito. O tamanho da ordem é `Volume + |Position|`, o que automaticamente inverte uma posição vendida se necessário.
4. Entrada vendida – simétrica à lógica comprada.
5. `StartProtection` opcional aplica stop loss e take profit usando pontos absolutos de preço.

## Gestão de risco

A estratégia ativa `StartProtection` uma vez que inicia. As distâncias são definidas em pontos de preço (`StopLevelPoints`, `TakeLevelPoints`) e são traduzidas com o `Security.PriceStep` atual. Ambos os valores podem ser definidos como zero para desabilitar a proteção correspondente. O parâmetro de tempo de espera evita re-entradas imediatas na mesma direção, emulando a configuração de vencimento de ordens pendentes do EA original.

## Parâmetros

| Parâmetro | Descrição | Valores padrão |
|-----------|-------------|---------|
| `CandleType` | Série de dados usada para análise. | Período de 15 minutos |
| `ThresholdOpen` | Pontuação ponderada mínima para abrir uma posição. | 55 |
| `ThresholdClose` | Pontuação oposta mínima para fechar uma posição. | 100 |
| `PriceLevelPoints` | Distância necessária entre preço e MA deslocada (em pontos). | 0 |
| `StopLevelPoints` | Distância do stop loss (pontos). | 50 |
| `TakeLevelPoints` | Distância do take profit (pontos). | 50 |
| `ExpirationBars` | Tempo de espera em barras antes de re-entrar na mesma direção. | 4 |
| `MaPeriod` | Período da média móvel. | 20 |
| `MaShift` | Deslocamento para frente aplicado à saída da MA (barras). | 3 |
| `MaMethods` | Método de média móvel (Simple, Exponential, Smoothed, LinearWeighted). | Simple |
| `MaAppliedPrice` | Fonte de preço para a MA. | Close |
| `MaWeight` | Peso atribuído à pontuação MA. | 0.8 |
| `RsiPeriod` | Período RSI. | 3 |
| `RsiAppliedPrice` | Fonte de preço para o RSI. | Close |
| `RsiWeight` | Peso atribuído à pontuação RSI. | 0.5 |

## Notas

* A estratégia funciona estritamente em velas finalizadas e ignora atualizações parciais.
* Definir ambos os pesos de indicador como zero desabilita o trading porque a pontuação combinada não pode mais atingir os limiares.
* Tempo de espera (`ExpirationBars`) igual a zero permite múltiplas entradas na mesma direção sem esperar.
* Como o StockSharp executa ordens a mercado por padrão, o vencimento de ordens pendentes do EA original é representado pelo mecanismo de tempo de espera em vez do cancelamento real de ordens.
