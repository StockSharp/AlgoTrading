# Estratégia Williams AO + AC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **estratégia Williams AO + AC** converte o expert do MetaTrader 4 "Williams_AOAC" para a API de estratégia de alto nível do StockSharp. A abordagem combina várias ferramentas de Bill Williams para encontrar rajadas de momentum no gráfico horário (período padrão):

1. **Filtro Bollinger Band** - a estratégia negocia apenas quando a largura da banda está dentro de uma faixa configurável de pontos, ajudando a evitar mercados laterais e volatilidade excessiva.
2. **Confirmação Relative Strength Index** - o RSI deve estar acima de um limite altista para compradas ou abaixo de um limite baixista para vendidas.
3. **Cruzamento da linha zero do Awesome Oscillator** - o oscilador deve cruzar o eixo zero na direção da operação, sinalizando mudança de momentum.
4. **Aceleração do Accelerator Oscillator** - os três últimos valores do Accelerator devem estar no mesmo lado de zero, e a barra mais recente deve estender esse movimento, confirmando aceleração.
5. **Filtro de sessão de negociação** - entradas são permitidas apenas dentro de uma janela de tempo configurável expressa em horas do dia.

Em cada candle concluído, a estratégia processa os valores dos indicadores entregues pelo pipeline `Bind`. Quando todos os filtros se alinham, ela fecha uma posição oposta se necessário e abre uma nova ordem a mercado com o tamanho de lote solicitado. Stop-loss e take-profit são aplicados usando distância em pontos de preço, e um trailing stop opcional pode apertar o stop de proteção depois que a operação se torna lucrativa.

## Regras de entrada
### Condições compradas
1. O spread de Bollinger (banda superior menos banda inferior convertido para pontos) está entre **BollingerSpreadLower** e **BollingerSpreadUpper**.
2. A leitura RSI é estritamente maior que **RsiBuyThreshold**.
3. Awesome Oscillator cruza de negativo para positivo na barra atual.
4. Valores do Accelerator Oscillator dos três últimos candles são todos positivos e o valor mais recente é maior que o anterior, sinalizando momentum altista crescente.
5. O horário de abertura da barra atual cai dentro da janela de negociação que começa em **EntryHour** e se estende por **TradingWindowHours** horas (atravessando a meia-noite se necessário).
6. A estratégia ainda não mantém uma posição comprada (pode estar zerada ou vendida).

Quando a lógica é satisfeita, a estratégia fecha qualquer exposição vendida, abre uma ordem comprada a mercado com **TradeVolume** e aplica as distâncias configuradas de stop-loss / take-profit. O acompanhamento de trailing stop começa depois que a operação se move a favor por pelo menos **TrailingStopPoints**.

### Condições vendidas
1. O spread de Bollinger está dentro da faixa permitida.
2. A leitura RSI é estritamente menor que **RsiSellThreshold**.
3. Awesome Oscillator cruza de positivo para negativo na barra atual.
4. Valores do Accelerator Oscillator dos três últimos candles são todos negativos e o valor mais recente é menor que o anterior, indicando pressão baixista crescente.
5. O horário de abertura do candle está dentro da janela de sessão de negociação.
6. A estratégia ainda não mantém uma posição vendida (pode estar zerada ou comprada).

Quando acionado, o módulo fecha exposição comprada, entra em uma ordem vendida a mercado com **TradeVolume** e reatribui as ordens de proteção.

## Gestão de saída
* **Take-profit** - se **TakeProfitPoints** for maior que zero, um alvo de lucro igual a essa quantidade de pontos de preço a partir do preço de entrada é anexado a cada nova posição.
* **Stop-loss** - se **StopLossPoints** for maior que zero, um stop fixo é aplicado em relação ao preço de entrada.
* **Trailing stop** - se **TrailingStopPoints** for maior que zero, o stop-loss é movido para mais perto do mercado quando o lucro excede a distância de trailing. Para operações compradas, o stop é elevado para `Close - TrailingStopPoints * pip`; para vendidas, é abaixado para `Close + TrailingStopPoints * pip`. O trailing é unilateral: o stop nunca volta.
* Mudanças manuais de posição pelo usuário são respeitadas; a lógica trailing reage à posição agregada atual reportada pelo mecanismo.

## Parâmetros
| Nome | Descrição | Padrão |
|------|-----------|--------|
| `CandleType` | Série de candles primária usada para cálculos. | Candles de 1 hora |
| `BollingerPeriod` | Período de retrospectiva das Bollinger Bands. | 20 |
| `BollingerDeviation` | Multiplicador de desvio padrão. | 2.0 |
| `BollingerSpreadLower` | Largura mínima da banda em pontos exigida para habilitar a negociação. | 40 |
| `BollingerSpreadUpper` | Largura máxima da banda em pontos permitida para negociação. | 210 |
| `AoFastPeriod` | Período curto do Awesome Oscillator. | 11 |
| `AoSlowPeriod` | Período longo do Awesome Oscillator. | 40 |
| `RsiPeriod` | Comprimento de cálculo RSI. | 20 |
| `RsiBuyThreshold` | Valor RSI mínimo para operações compradas. | 46 |
| `RsiSellThreshold` | Valor RSI máximo para operações vendidas. | 40 |
| `EntryHour` | Hora (0-23) em que a janela de negociação começa. | 0 |
| `TradingWindowHours` | Duração da janela de negociação permitida em horas (`0` mantém apenas a hora inicial). | 20 |
| `TradeVolume` | Tamanho de lote para cada nova posição. | 0.01 |
| `StopLossPoints` | Distância de stop-loss em pontos de preço. | 60 |
| `TakeProfitPoints` | Distância de take-profit em pontos de preço. | 90 |
| `TrailingStopPoints` | Distância de trailing stop em pontos de preço. | 30 |

## Observações adicionais
* O valor do Accelerator Oscillator é derivado internamente subtraindo uma média móvel simples de 5 períodos do Awesome Oscillator da leitura AO atual, correspondendo à implementação MetaTrader usada pelo expert original.
* Os cálculos de spread da banda dependem do `PriceStep` do instrumento. Quando indisponível, a estratégia recorre a diferenças de preço brutas.
* A janela de sessão de negociação atravessa a meia-noite quando `EntryHour + TradingWindowHours` excede 23, reproduzindo o filtro horário do MetaTrader.
* A estratégia fecha automaticamente exposição oposta antes de abrir uma nova posição, replicando o limite de uma única ordem do código MQL4 original.
