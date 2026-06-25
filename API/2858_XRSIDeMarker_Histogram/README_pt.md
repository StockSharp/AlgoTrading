# Estratégia de Histograma XRSI DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
Esta estratégia replica o assessor especialista **Exp_XRSIDeMarker_Histogram**. Opera reversões detectadas por um oscilador personalizado que combina um Índice de Força Relativa (RSI) com o indicador DeMarker e depois suaviza o resultado. O sistema pode abrir ou fechar trades longos e curtos de forma independente, e stops protetores opcionais expressos em passos de preço são suportados.

## Construção do indicador
1. **Preço aplicado** – o RSI é calculado na entrada selecionada (preço de fechamento, abertura, máximo, mínimo, mediano, típico ou ponderado) usando o período configurado.
2. **Componente DeMarker** – para cada vela terminada, a estratégia mede a pressão ascendente (`deMax`) e descendente (`deMin`):
   - `deMax = max(High_t - High_{t-1}, 0)`
   - `deMin = max(Low_{t-1} - Low_t, 0)`
   Ambas as séries são suavizadas com uma média móvel simples cujo comprimento corresponde ao período do RSI.
   - `DeMarker = deMaxAvg / (deMaxAvg + deMinAvg)` (escalado para o intervalo 0–100).
3. **Oscilador composto** – o valor final é `(RSI + 100 * DeMarker) / 2`.
4. **Suavização** – o oscilador composto passa por uma das médias móveis suportadas (SMA, EMA, SMMA, LWMA ou Jurik). Se um modo não suportado da versão MQL original for selecionado, o indicador reverte para uma EMA com o comprimento solicitado. A opção Jurik também respeita o parâmetro de fase.
5. **Histórico de sinais** – a estratégia armazena valores históricos e avalia sinais na barra definida por `SignalBar`, imitando o EA original que aguardava a próxima vela antes de operar.

## Lógica de trading
- **Reversão altista**
  - Condição: valor em `SignalBar+1` é menor que em `SignalBar+2` (inclinação descendente) e o valor em `SignalBar` volta a subir (`>=`).
  - Ações:
    - Fechar trades curtos existentes quando `CloseShortOnLongSignal` é verdadeiro.
    - Abrir um novo trade longo com `TradeVolume` (mais a quantidade necessária para inverter de um curto) quando `AllowBuyEntries` está habilitado.
- **Reversão baixista**
  - Condição: valor em `SignalBar+1` é maior que em `SignalBar+2` (inclinação ascendente) e o valor em `SignalBar` desce (`<=`).
  - Ações:
    - Fechar trades longos existentes quando `CloseLongOnShortSignal` é verdadeiro.
    - Abrir um novo trade curto quando `AllowSellEntries` está habilitado.
- Os sinais são ignorados até que o indicador e os componentes DeMarker estejam completamente formados, e as ordens são colocadas somente quando a estratégia está online e o trading é permitido.

## Gestão de risco
- `StopLossTicks` e `TakeProfitTicks` representam distâncias em **passos de preço**. A estratégia multiplica esses valores por `Security.PriceStep` (usando `1` se o passo do instrumento for desconhecido) e fecha a posição quando a distância é atingida dentro do intervalo da vela.
- Passar `0` desativa a proteção respectiva.
- O parâmetro `TradeVolume` é usado como tamanho de ordem padrão e também para calcular reversões (a posição oposta é fechada antes que uma nova seja aberta).

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `TradeVolume` | Volume ao abrir novas posições. | `0.1` |
| `StopLossTicks` | Stop protetor em passos de preço. | `1000` |
| `TakeProfitTicks` | Alvo de lucro em passos de preço. | `2000` |
| `AllowBuyEntries` | Habilitar/desabilitar entradas longas. | `true` |
| `AllowSellEntries` | Habilitar/desabilitar entradas curtas. | `true` |
| `CloseLongOnShortSignal` | Fechar comprados quando um sinal curto aparece. | `true` |
| `CloseShortOnLongSignal` | Fechar vendidos quando um sinal longo aparece. | `true` |
| `CandleType` | Período usado para análise (velas de 4 horas por padrão). | `H4` |
| `IndicatorPeriod` | Retrospecto para os componentes RSI e DeMarker. | `14` |
| `AppliedPriceSelection` | Preço aplicado usado pelo cálculo do RSI. | `Close` |
| `SmoothingMethodSelection` | Média móvel usada para suavização (SMA/EMA/SMMA/LWMA/Jurik/Adaptive). | `Sma` |
| `SmoothingLength` | Período da média de suavização. | `5` |
| `SmoothingPhase` | Argumento de fase passado para a suavização Jurik. | `15` |
| `SignalBar` | Número de barras fechadas atrás usadas para avaliação de sinais. | `1` |

## Notas vs. EA original
- Os modos de gestão monetária da versão MQL (baseado em saldo, margem livre, etc.) são substituídos por um parâmetro direto `TradeVolume`.
- O deslizamento de ordens (`Deviation`) não é necessário porque o StockSharp usa ordens de mercado.
- Algoritmos de suavização avançados (MA Parabólico, T3, VIDYA, AMA) não estão disponíveis no StockSharp e são mapeados para EMA através da opção `Adaptive`.
- Todos os comentários no código-fonte C# são escritos em inglês, e a lógica é executada apenas em velas terminadas, assim como a implementação original.
