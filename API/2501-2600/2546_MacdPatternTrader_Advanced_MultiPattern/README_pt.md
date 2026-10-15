# Estratégia MacdPatternTrader Avançado MultiPadrão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia MacdPatternTrader é uma conversão de alto nível do StockSharp do consultor especialista MQL original *MacdPatternTraderAll*. O sistema ouve velas completadas e avalia seis padrões de entrada independentes baseados em MACD. Cada padrão usa suas próprias médias móveis exponenciais rápidas e lentas mais níveis de limiar dedicados para reconhecer estruturas de reversão e continuação na linha principal do MACD. Os sinais podem chegar simultaneamente e cada um submete uma ordem de mercado dimensionada pelo volume martingale atual.

A estratégia complementa a lógica de entrada com gestão de risco adaptativa. Os preços de stop-loss são calculados a partir de máximas ou mínimas recentes com um offset, enquanto os alvos de take-profit se estendem por blocos de histórico sucessivos da mesma maneira que a implementação MQL. As posições abertas são gerenciadas ativamente por saídas parciais baseadas em filtros EMA/SMA e um limite de lucro não realizado. Após cada fechamento plano, o multiplicador martingale é redefinido ou duplica o tamanho do lote dependendo do resultado realizado.

## Regras de trading
1. **Padrão 1 – Reversão por limiar**
   * Rastreia quando a linha principal do MACD sobe acima de um limiar superior, depois vira para baixo enquanto permanece positiva.
   * Espelha o comportamento para o limiar inferior quando o MACD se recupera do território negativo.
2. **Padrão 2 – Bounce do nível zero**
   * Requer uma fase MACD positiva, então um gancho baixista abaixo da linha zero antes de vender.
   * Usa a lógica simétrica para ganchos altistas acima de zero para comprar.
3. **Padrão 3 – Sequência de múltiplos estágios**
   * Reproduz o reconhecimento de crista e vale em três estágios do código fonte MQL usando flags aninhados e pares de limiares.
   * Reinicia os contadores auxiliares (`bars_bup`) após cada operação executada.
4. **Padrão 4 – Pico/vale local**
   * Aguarda máximas ou mínimas locais do MACD em relação às duas barras anteriores para configurar sinais vendidos e comprados respectivamente.
5. **Padrão 5 – Rompimento de banda neutra**
   * Busca entradas vendidas após cair abaixo de uma banda neutra e imediatamente retornar abaixo de um limite baixista.
   * Busca entradas compradas após mover acima da banda neutra e saltar sobre um limite altista.
6. **Padrão 6 – Contador de barras consecutivas**
   * Conta o número de barras acima ou abaixo dos limiares configurados e só aciona quando o contador excede o valor `TriggerBars` enquanto permanece abaixo do limite `MaxBars`.

## Gestão de risco e gestão de operações
* **Stop-loss** – Determinado pelo preço mais alto (para operações vendidas) ou mais baixo (para operações compradas) durante as últimas velas `StopLossBars` mais o offset configurado traduzido em unidades de passo de preço.
* **Take-profit** – Pesquisa segmentos de histórico consecutivos de velas `TakeProfitBars`, exatamente como os loops `iLowest`/`iHighest` aninhados na versão MQL. O alvo se estende enquanto o próximo segmento produz um valor mais extremo.
* **Saídas parciais** – Uma vez que o lucro não realizado excede cinco unidades monetárias (aproximado por diferença de preço × volume de posição) e os filtros EMA/SMA concordam, a estratégia fecha um terço do volume aberto, então metade do restante.
* **Controle de lote martingale** – Após um fechamento plano a estratégia reinicia o lote para `InitialVolume` quando a operação fechada ganhou dinheiro; caso contrário, o volume dobra (se `UseMartingale` estiver habilitado).
* **Filtro de tempo** – Quando `UseTimeFilter` está habilitado a estratégia só avalia entradas dentro da janela `(StartTime, StopTime)`. Os stops ainda são verificados em cada vela terminada.

## Parâmetros
| Grupo | Nome | Descrição |
| --- | --- | --- |
| Padrão 1 | `Pattern1Enabled` | Habilita o primeiro padrão MACD. |
| Padrão 1 | `Pattern1StopLossBars`, `Pattern1TakeProfitBars`, `Pattern1Offset` | Configurações de lookback e offset de stop-loss/take-profit. |
| Padrão 1 | `Pattern1Slow`, `Pattern1Fast` | Comprimentos EMA lentos e rápidos para o cálculo MACD. |
| Padrão 1 | `Pattern1MaxThreshold`, `Pattern1MinThreshold` | Limiares MACD superiores e inferiores. |
| Padrão 2 | Mesma estrutura que o padrão 1 com seus próprios valores. |
| Padrão 3 | Adiciona limiares extras `Pattern3MaxLowThreshold` e `Pattern3MinHighThreshold` para reproduzir o reconhecimento de crista/vale em camadas. |
| Padrão 4 | Inclui `Pattern4AdditionalBars` (mantido para compatibilidade com o código original). |
| Padrão 5 | Usa limiares neutros para detecção de rompimento de banda. |
| Padrão 6 | Adiciona `Pattern6MaxBars`, `Pattern6MinBars`, `Pattern6TriggerBars` para gerenciar a lógica do contador de barras. |
| Gestão | `EmaPeriod1`, `EmaPeriod2`, `SmaPeriod3`, `EmaPeriod4` | Médias móveis para filtros de saída parcial. |
| Geral | `InitialVolume`, `UseTimeFilter`, `StartTime`, `StopTime`, `UseMartingale`, `CandleType` | Controles de comportamento global. |

## Notas
* A conversão mantém a estrutura lógica original, incluindo a pesquisa segmentada de take-profit e as regras de redefinição martingale.
* As saídas parciais baseadas em lucro usam uma aproximação porque a API de alto nível do StockSharp não expõe valores brutos de lucro do terminal por posição; em vez disso, é usada diferença de preço × volume.
* `Pattern4AdditionalBars` é preservado para compatibilidade, embora o código MQL original nunca o tenha referenciado diretamente.
* Stops e take-profits são avaliados em velas fechadas porque o StockSharp não anexa ordens protetoras automaticamente na API de alto nível.
