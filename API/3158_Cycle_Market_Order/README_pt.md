# Estratégia de Cycle Market Order
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Convertida do consultor especialista MetaTrader 4 "CycleMarketOrder_V181". A estratégia organiza um número fixo de slots dentro de uma escada de preços e abre ordens de mercado quando o bid/ask ao vivo negocia através de um slot individual. Cada slot carrega seu próprio volume, limiar de ponto de equilíbrio e valor de trailing stop, para que o grid possa gradualmente escalonar em uma posição enquanto protege lucros que já atingiram a distância necessária.

## Lógica de trading

1. O tamanho do pip é derivado do passo de preço do instrumento e da precisão decimal (símbolos de 5/3 dígitos mapeiam para 10 pontos por pip). Os parâmetros `MaxPrice`, `SpanPips` e `MaxCount` são então usados para pré-calcular o intervalo de preços gerenciado por cada slot.
2. Os dados de mercado de nível 1 são consumidos para imitar o comportamento baseado em ticks do Expert Advisor original. Cada atualização atualiza os preços de melhor bid/ask em cache.
3. Se `UseWeekendMode` estiver habilitado, a estratégia recusa negociar fora da janela de fim de semana configurada (sábado a partir de `WeekendHour`, o domingo inteiro e segunda-feira antes de `WeekstartHour`).
4. Para ciclos longos (`EntryDirection = 1`), o algoritmo escaneia slots do identificador mais baixo ao mais alto. Sempre que o preço ask atual cair entre `startPrice` e `endPrice` do slot, uma ordem de compra de mercado com volume `OrderVolume` é enviada. Ciclos curtos (`EntryDirection = -1`) espelham essa lógica e usam o preço bid.
5. Os estados dos slots rastreiam ordens de entrada/saída pendentes, volume preenchido e o preço médio de entrada. O registro usa `MagicNumberBase + index` para corresponder aos identificadores "mágicos" do MT4.
6. O gerenciamento do trailing é executado em cada atualização de nível 1 antes de avaliar novas entradas. Uma vez que o lucro em um slot longo excede `BreakEvenPips + TrailingStopPips`, o stop é empurrado para `Bid - TrailingStopPips`. Slots curtos usam `Ask + TrailingStopPips` e a condição de ponto de equilíbrio espelhada. Quando o preço de mercado cruza o stop armazenado, o slot é fechado com uma ordem de mercado.
7. Como apenas ordens de mercado são usadas, não há ordens pendentes para cancelar. Preenchimentos parciais ajustam o volume restante do slot para que a estratégia possa continuar fazendo trailing ou re-armar o slot quando ficar neutro.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `EntryDirection` | Direção de trading: `1` compra a escada, `-1` a vende, `0` desabilita novas entradas enquanto mantém o trailing ativo. |
| `MaxPrice` | Preço âncora superior usado para calcular os intervalos de slots. |
| `MaxCount` | Número total de slots ativos dentro do grid. |
| `SpanPips` | Distância em pips entre limites de slots consecutivos. |
| `OrderVolume` | Volume enviado quando um slot é acionado. |
| `BreakEvenPips` | Distância de lucro que deve ser excedida antes que o trailing stop seja armado. |
| `TrailingStopPips` | Distância de trailing aplicada após o ponto de equilíbrio ser atingido. |
| `UseWeekendMode` | Habilita a janela de bloqueio de trading de fim de semana. |
| `WeekendHour` | Hora do sábado (horário do terminal) quando o trading é interrompido. |
| `WeekstartHour` | Hora da segunda-feira quando o trading é retomado. |
| `MagicNumberBase` | Deslocamento de identificador usado em mensagens de log para corresponder aos números mágicos originais. |

## Notas de implementação

* O gerenciamento de slots rastreia ordens de entrada e saída pendentes para que preenchimentos repetidos não registrem volume duplicado.
* A estratégia reinicia seu trailing stop sempre que um novo preenchimento aumenta a exposição do slot, garantindo que o stop reflita o preço médio de entrada mais recente.
* A proteção de fim de semana simplesmente pula tanto a lógica de trailing quanto de entrada; posições existentes permanecem intocadas enquanto o bloqueio está ativo.
* Os dados de nível 1 são necessários porque a lógica compara preços brutos de bid/ask em vez de fechamentos de velas, replicando de perto o comportamento tick a tick da versão MT4.
