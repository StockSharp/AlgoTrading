# RSI Bollinger Bands EA (Conversão StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um porto de alto nível do StockSharp do assessor especialista MetaTrader 5 "RSI Bollinger Bands EA". Opera em velas de 15 minutos e combina dois gatilhos independentes baseados em RSI:

* **Gatilho Um** – limares fixos de sobrecompra/sobrevenda para RSI em M15, H1 e H4 junto com uma confirmação estocástica e filtro de inclinação.
* **Gatilho Dois** – bandas RSI adaptativas calculadas a partir de desvios padrão assimétricos (sigma positivo/negativo separado) sobre tamanhos de amostra configuráveis nos três períodos. O RSI deve perfurar as bandas dinâmicas enquanto o estocástico confirma o momentum.

Ambos os gatilhos requerem contração de volatilidade no período inferior (spread de Bollinger em M15), expansão de volatilidade no período superior (spread de Bollinger em H4) e um ambiente tranquilo de acordo com o ATR de H4. Apenas um gatilho pode ser habilitado de cada vez, espelhando as restrições do EA original.

## Requisitos de dados
* Velas de execução primária: `M15CandleType` (padrão: 15 minutos). Todas as entradas e saídas são avaliadas no fechamento dessas velas.
* Velas de confirmação: `H1CandleType` (padrão: 1 hora) para condições RSI e estatísticas.
* Velas de período superior: `H4CandleType` (padrão: 4 horas) para verificações RSI, filtro de spread de Bollinger e filtro de volatilidade ATR.

## Lógica de trading
1. **Filtros de sessão**
   * O trading é limitado a uma janela de tempo configurável que começa em `EntryHour` e dura `OpenHours` horas. Quando `OpenHours` é zero, a janela dura pela hora de abertura única.
   * O trading para nas sextas-feiras assim que a hora da vela alcança `FridayEndHour` (padrão: 4, ou seja, após as 04:00 de sexta-feira).
   * Uma nova posição só pode ser aberta quando a posição líquida atual está plana (`Position == 0`).

2. **Filtros de volatilidade e spread (ambos os gatilhos)**
   * O spread de Bollinger H4 deve ser maior que `BbSpreadH4MinX` pips (X = 1 ou 2) para garantir que o intervalo do período superior seja amplo o suficiente.
   * O spread de Bollinger M15 deve permanecer abaixo de `BbSpreadM15MaxX` pips para que o preço esteja comprimido no período de trading.
   * O ATR H4 convertido em pips deve permanecer abaixo de `AtrLimit`.

3. **Gatilho Um – níveis RSI fixos**
   * Os valores RSI para M15, H1 e H4 devem cair abaixo de seus respectivos limiares "Low" para acionar uma configuração comprada, enquanto permanecem acima dos fail-safes "Low Limit".
   * O RSI deve subir em relação à leitura M15 anterior em mais de `RDeltaM15Lim1` (padrão: –3.5 pips) para posições compradas, ou cair mais do que o limiar espelhado para posições vendidas.
   * A linha principal estocástica deve estar abaixo de `StocLoM15_1` para compradas ou acima de `StocHiM15_1` para vendidas.
   * As entradas vendidas requerem que os valores RSI estejam acima de seus limiares "High" mas permaneçam abaixo dos fail-safes "High Limit".

4. **Gatilho Dois – bandas sigma RSI adaptativas**
   * Amostras RSI históricas são mantidas para cada período (até `NumRsi` elementos). Desvios padrão positivos e negativos separados são calculados para construir bandas assimétricas semelhantes a Bollinger.
   * Bandas inferiores e superiores dinâmicas para cada período são produzidas aplicando multiplicadores `Rsi*M*Sigma2` (M15/H1/H4). Multiplicadores "limite" adicionais (`Rsi*M*SigmaLim2`) limitam os extremos permitidos.
   * As entradas compradas requerem que todos os três valores RSI estejam abaixo de suas respectivas bandas inferiores adaptativas mas acima dos limites inferiores. O estocástico deve estar abaixo de `StocLoM15_2` e a inclinação RSI deve ser maior que `RDeltaM15Lim2`.
   * As entradas vendidas espelham a lógica com bandas superiores e limiares.

5. **Execução de ordens e saídas**
   * Quando um gatilho dispara, uma ordem de mercado de tamanho `Volume` (padrão: 0.1 lotes) é colocada.
   * Os preços de stop-loss e take-profit são derivados das distâncias em pips configuradas para o gatilho ativo (`StopLoss*X`, `TakeProfit*X`) usando a heurística de tamanho de pip do instrumento (símbolos de 5 dígitos e 3 dígitos recebem escalonamento de 10x).
   * As saídas de proteção são simuladas na próxima vela M15: se a máxima/mínima da vela toca o stop ou o nível de take-profit, a estratégia fecha a posição a mercado e redefine os preços de proteção. Isso imita o comportamento do MT5 que dependia de ordens stop.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `Volume` | Volume de trade em lotes. | `0.1` |
| `TriggerOne` | Habilitar o gatilho RSI fixo. | `true` |
| `TriggerTwo` | Habilitar o gatilho RSI adaptativo (mutuamente exclusivo com o gatilho um). | `false` |
| `BbSpreadH4Min1` | Spread mínimo de Bollinger H4 (pips) para o gatilho um. | `84` |
| `BbSpreadM15Max1` | Spread máximo de Bollinger M15 (pips) para o gatilho um. | `64` |
| `RsiPeriod1` | Comprimento RSI usado pelo gatilho um em todos os períodos. | `10` |
| `RsiLoM15_1`, `RsiHiM15_1` | Limiares RSI para M15. | `24`, `66` |
| `RsiLoH1_1`, `RsiHiH1_1` | Limiares RSI para H1. | `34`, `54` |
| `RsiLoH4_1`, `RsiHiH4_1` | Limiares RSI para H4. | `48`, `56` |
| `RsiLoLim*`, `RsiHiLim*` | Limites de segurança para bloquear leituras RSI extremas. | `20–92` |
| `RDeltaM15Lim1` | Inclinação mínima RSI em M15 para o gatilho um. | `-3.5` |
| `StocLoM15_1`, `StocHiM15_1` | Limites estocásticos para o gatilho um. | `26`, `64` |
| `BbSpreadH4Min2` | Spread mínimo de Bollinger H4 (pips) para o gatilho dois. | `65` |
| `BbSpreadM15Max2` | Spread máximo de Bollinger M15 (pips) para o gatilho dois. | `75` |
| `RsiPeriod2` | Comprimento RSI usado pelo gatilho dois. | `20` |
| `NumRsi` | Tamanho da amostra para estatísticas RSI. | `60` |
| `Rsi*M*Sigma2` | Multiplicadores para bandas adaptativas principais (M15/H1/H4). | `1.20 / 0.95 / 0.9` |
| `Rsi*M*SigmaLim2` | Multiplicadores para limites externos (M15/H1/H4). | `1.85 / 2.55 / 2.7` |
| `RDeltaM15Lim2` | Inclinação mínima RSI em M15 para o gatilho dois. | `-5.5` |
| `StocLoM15_2`, `StocHiM15_2` | Limites estocásticos para o gatilho dois. | `24`, `68` |
| `TakeProfitBuy1`, `StopLossBuy1` | Distâncias de proteção em pips para comprados do gatilho um. | `150`, `70` |
| `TakeProfitSell1`, `StopLossSell1` | Distâncias de proteção em pips para vendidos do gatilho um. | `70`, `35` |
| `TakeProfitBuy2`, `StopLossBuy2` | Distâncias de proteção em pips para comprados do gatilho dois. | `140`, `35` |
| `TakeProfitSell2`, `StopLossSell2` | Distâncias de proteção em pips para vendidos do gatilho dois. | `60`, `30` |
| `AtrPeriod` | Período de cálculo ATR H4. | `60` |
| `BollingerPeriod` | Comprimento de Bollinger Bands em M15 e H4. | `20` |
| `AtrLimit` | ATR máximo em pips para permitir entradas. | `90` |
| `EntryHour` | Hora de início da sessão (0–23). | `0` |
| `OpenHours` | Duração da sessão em horas (0 = uma hora). | `14` |
| `NumPositions` | Máximo de posições líquidas simultâneas (a estratégia abre apenas quando plana). | `1` |
| `FridayEndHour` | Hora de sexta-feira após a qual o trading para. | `4` |
| `StochasticK`, `StochasticD`, `StochasticSlowing` | Parâmetros para o oscilador estocástico. | `12 / 5 / 5` |
| `M15CandleType`, `H1CandleType`, `H4CandleType` | Tipos de dados de velas para cada período. | `15m / 1h / 4h` |

## Notas
* As ordens protetoras de stop-loss e take-profit do EA original são emuladas monitorando as máximas/mínimas das velas M15. Se a precisão de tick intrabarra for necessária, considere adicionar ordens stop através da API de baixo nível.
* Certifique-se de que o portfólio forneça acesso a todos os períodos solicitados; caso contrário, as filas de gatilhos não se formarão e a estratégia permanecerá inativa.
* A heurística de tamanho de pip segue a convenção comum do MetaTrader: símbolos de 5 dígitos (ou 3 dígitos para cruzamentos de JPY) multiplicam o `PriceStep` da bolsa por 10.
