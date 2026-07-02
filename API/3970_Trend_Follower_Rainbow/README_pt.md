# Estratégia do arco-íris do seguidor de tendências
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Trend Follower Rainbow Strategy é uma versão C# do consultor especialista MetaTrader 4 "TrendFollowerRainbowMethodkyast773". A estratégia combina várias camadas de confirmação para negociar na direção de tendências fortes, ao mesmo tempo que filtra os períodos limitados. Ele se baseia no alinhamento de um arco-íris de médias móveis exponenciais, impulso MACD, limites do oscilador Laguerre, leituras do índice de fluxo de dinheiro e um cruzamento rápido/EMA lenta para acionar posições.

## Lógica de negociação
1. **Janela de negociação** – Os sinais são avaliados apenas quando o tempo de fechamento da vela atual está estritamente entre os horários de início e término configuráveis. Isso imita o filtro de tempo original do EA que evitou o primeiro e o último horário de negociação da sessão.
2. **EMA Gatilho de cruzamento** – Uma configuração longa requer que o EMA rápido (comprimento padrão 4) cruze acima do EMA lento (comprimento padrão 8). Uma configuração curta requer o cruzamento oposto.
3. **MACD Confirmação** – A linha MACD e a linha de sinal (padrão 35/05/5) devem estar acima de zero para negociações longas ou abaixo de zero para negociações curtas para confirmar o alinhamento do impulso.
4. **Filtro Laguerre** – O valor do filtro Laguerre deve ultrapassar 0,15 para negociações longas ou abaixo de 0,75 para negociações curtas, reproduzindo as verificações de limite originais realizadas no indicador personalizado.
5. **Alinhamento do arco-íris** – Cinco pacotes de médias móveis exponenciais (quatro EMAs por pacote) devem ser classificados monotonicamente para confirmar a estrutura do arco-íris. Os pacotes são avaliados para ordem não crescente em cenários de alta e ordem não decrescente em cenários de baixa.
6. **Filtro do Índice de Fluxo de Dinheiro** – O Índice de Fluxo de Dinheiro (período padrão 14) deve estar abaixo de 40 para entradas longas e acima de 60 para entradas curtas para evitar negociação contra o fluxo orientado por volume.
7. **Gerenciamento de posição** – São utilizadas ordens de mercado. Quando aparece um sinal oposto, a exposição existente é fechada e uma nova posição é aberta na direção oposta.

## Gestão de risco
A estratégia oferece suporte a proteções integradas por meio do auxiliar `StartProtection` de StockSharp:
- As distâncias de **Take Profit** e **Stop Loss** são expressas em etapas de preço para espelhar a configuração baseada em pontos do EA.
- A distância **Trailing Stop** também usa etapas de preço e é ativada assim que o bloqueio de proteção é iniciado.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `OrderVolume` | Volume base de ordens de mercado. | 1 |
| `TakeProfitPoints` | Calcule a distância do lucro nas etapas de preço. | 17 |
| `StopLossPoints` | Pare a distância de perda nas etapas de preço. | 30 |
| `TrailingStopPoints` | Distância do trailing stop em etapas de preço. | 45 |
| `TradingStartHour` | Primeira hora (inclusive) que é ignorada antes da avaliação dos sinais. | 1 |
| `TradingEndHour` | Última hora (inclusive) que é ignorada após a avaliação dos sinais. | 23 |
| `FastEmaLength` | Comprimento do EMA rápido usado no gatilho de cruzamento. | 4 |
| `SlowEmaLength` | Comprimento do EMA lento usado no gatilho de cruzamento. | 8 |
| `MacdFastLength` | MACD comprimento EMA rápido. | 5 |
| `MacdSlowLength` | MACD comprimento EMA lento. | 35 |
| `MacdSignalLength` | MACD comprimento do sinal EMA. | 5 |
| `LaguerreGamma` | Fator de suavização do filtro Laguerre. | 0,7 |
| `LaguerreBuyThreshold` | O limite de Laguerre ultrapassou para cima em negociações longas. | 0,15 |
| `LaguerreSellThreshold` | O limite de Laguerre ultrapassou para baixo para negociações curtas. | 0,75 |
| `MfiPeriod` | Período de cálculo do Índice de Fluxo de Dinheiro. | 14 |
| `MfiBuyLevel` | Nível máximo de IMF que ainda permite entradas longas. | 40 |
| `MfiSellLevel` | Nível mínimo de IMF que ainda permite entradas curtas. | 60 |
| `RainbowGroup{1..5}Base` | Comprimento base de EMA para cada pacote arco-íris. Quatro EMAs consecutivos são criados a partir de cada valor base adicionando compensações (0, 2, 4, 6). | 5/13/21/34/55 |
| `CandleType` | Série de velas primárias usadas pela estratégia. O padrão é velas de 5 minutos. | Período de 5 minutos |

## Gráficos
A estratégia desenha automaticamente:
- Velas de preço para a série assinada.
- EMAs rápidos e lentos para confirmação visual de cruzamentos.
- Valores do filtro Laguerre para observar cruzamentos de limites.
- Negociações próprias plotadas na área do gráfico.

## Notas
- A lógica do arco-íris se aproxima dos indicadores personalizados originais do RainbowMMA construindo pacotes EMA configuráveis. Ajuste os comprimentos da base para corresponder a um modelo de arco-íris específico, se necessário.
- Todos os comentários de código, logs e documentação são fornecidos em inglês, conforme necessário.
- A estratégia concentra-se exclusivamente na implementação do C#. Nenhuma porta Python é gerada nesta tarefa.
