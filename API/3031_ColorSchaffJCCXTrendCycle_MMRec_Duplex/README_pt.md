# Estratégia Color Schaff JCCX Trend Cycle MMRec Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Recria o Expert Advisor bidirecional "ColorSchaffJCCXTrendCycle_MMRec_Duplex" do MetaTrader dentro do StockSharp.
- Usa dois stacks independentes de Schaff Trend Cycle construídos sobre médias móveis Jurik para detectar reversões de alta e de baixa.
- Implementa um módulo MMRec simplificado (recomendador de gestão monetária) que reduz o tamanho após perdas repetidas.
- Aplica conjuntos de parâmetros separados para operações compradas e vendidas, possibilitando configurações assimétricas entre períodos e fontes de preço.

## Pipeline de indicadores
1. **Aproximação JCCX** – cada preço é processado por uma média móvel Jurik para obter uma série detendenciada. A série detendenciada e seu valor absoluto são suavizados novamente com médias Jurik para aproximar o oscilador JCCX original.
2. **Camada MACD** – a diferença entre as saídas JCCX rápida e lenta fornece a base de momentum.
3. **Transformação estocástica dupla** – janelas deslizantes de mínimo/máximo normalizam o momentum MACD e produzem o valor final do Schaff Trend Cycle (STC) no intervalo -100..+100.
4. **Controle de fase** – o parâmetro `Phase` modula um fator de suavização interno (0.05–0.95) aplicado após cada etapa estocástica, emulando o comportamento de "fase" do Jurik.

O stack de indicadores é executado duas vezes: uma para o bloco comprado e uma para o bloco vendido. Cada bloco pode usar diferentes tipos de candles e entradas de preço.

## Lógica de negociação
### Bloco comprado
- **Entrada**: quando o STC comprado cruza acima de zero (valor atual > 0 e o valor anterior atrasado ≤ 0). Posições vendidas existentes são fechadas primeiro.
- **Saída**: quando o STC comprado cai abaixo de zero e as saídas compradas estão habilitadas.
- **Stops**: distâncias opcionais de stop-loss e take-profit (expressas em passos de preço) são avaliadas em cada candle concluído usando máximas/mínimas do candle.

### Bloco vendido
- **Entrada**: quando o STC vendido cruza abaixo de zero (valor atual < 0 e o valor atrasado ≥ 0). Qualquer posição comprada existente é encerrada antes de abrir uma posição vendida.
- **Saída**: quando o STC vendido sobe acima de zero e as saídas vendidas estão habilitadas.
- **Stops**: verificações simétricas de stop-loss e take-profit para operações vendidas.

O parâmetro `SignalBar` define quantos candles completamente fechados são ignorados antes de os sinais serem avaliados. Um valor de `1` reproduz o comportamento do MetaTrader de usar o candle concluído anterior.

## Gestão monetária (MMRec)
- Duas filas rastreiam os resultados de operações mais recentes para comprados e vendidos.
- `TotalTrigger` limita o comprimento da fila; apenas os últimos N resultados são considerados.
- `LossTrigger` define quantas perdas dentro dessa fila mudam o tamanho da operação para `SmallVolume`.
- Quando o limite de perdas não é ultrapassado, a estratégia usa `NormalVolume`.

## Parâmetros
| Grupo | Parâmetro | Descrição | Padrão |
| --- | --- | --- | --- |
| Long | `LongCandleType` | Tipo de candle (período) para cálculos comprados. | Período de 8 horas |
| Long | `LongFastLength` | Comprimento Jurik rápido na aproximação JCCX comprada. | 23 |
| Long | `LongSlowLength` | Comprimento Jurik lento para a aproximação JCCX comprada. | 50 |
| Long | `LongSmoothLength` | Comprimento de suavização Jurik aplicado ao numerador/denominador. | 8 |
| Long | `LongPhase` | Parâmetro de fase traduzido em fator de suavização (0.05–0.95). | 100 |
| Long | `LongCycle` | Comprimento da janela deslizante para as transformações estocásticas. | 10 |
| Long | `LongSignalBar` | Atraso (em barras) antes de um sinal ser avaliado. | 1 |
| Long | `LongAppliedPrice` | Fonte de preço para cálculos comprados. | Close |
| Long | `LongAllowOpen` / `LongAllowClose` | Habilitar/desabilitar entradas ou saídas compradas. | true |
| Long | `LongTotalTrigger` | Número de operações compradas recentes armazenadas para a fila MMRec. | 5 |
| Long | `LongLossTrigger` | Perdas necessárias dentro da fila para mudar para volume pequeno. | 3 |
| Long | `LongSmallVolume` / `LongNormalVolume` | Tamanhos de operação comprada reduzido e padrão. | 0.01 / 0.1 |
| Long | `LongStopLoss` / `LongTakeProfit` | Distâncias opcionais de stop/take em passos de preço. | 1000 / 2000 |
| Short | Igual ao comprado (com prefixo `Short`). | | |

## Notas de risco
- Os passos de preço são obtidos do `Security` atual. Certifique-se de que o instrumento tem um `PriceStep` válido ou ajuste os parâmetros adequadamente.
- As verificações de stop-loss e take-profit são avaliadas em candles concluídos; a qualidade de execução intrabarra depende da resolução do candle.
- O módulo MMRec depende da comparação de preços de entrada e saída. Em negociação ao vivo, o slippage pode alterar o resultado efetivo.

## Dicas de uso
- Comece com configurações idênticas de comprado/vendido para emular o EA duplex original, depois experimente com períodos assimétricos.
- Reduza `SignalBar` para zero para respostas mais rápidas; aumente-o para filtrar ruído.
- Otimize `LongPhase`/`ShortPhase` junto com os comprimentos de suavização para ajustar a capacidade de resposta versus suavidade.
