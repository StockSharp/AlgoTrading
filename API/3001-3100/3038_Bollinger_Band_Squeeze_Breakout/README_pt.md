# Estratégia Bollinger Band Squeeze Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia replica o expert advisor original do MetaTrader 4 "BOLINGER BAND SQUEEZE" usando a API de alto nível do StockSharp. Procura por períodos onde as Bollinger Bands se contraem e então entra em operações assim que as bandas se expandem, desde que os filtros de momentum e tendência confirmem o movimento. A conversão mantém a lógica de confirmação multi-períodos e transforma os blocos de gestão monetária em expressões idiomáticas do StockSharp.

## Lógica de negociação
1. **Squeeze e expansão de bandas**
   - As Bollinger Bands (comprimento 20, desvio 2 por padrão) são calculadas no período de trabalho.
   - A largura do candle concluído mais recente é comparada contra a largura `RetraceCandles` barras atrás.
   - Um breakout válido requer que a razão de largura exceda `SqueezeRatio`, sinalizando que o preço está se expandindo para fora do squeeze.
2. **Filtro de tendência**
   - Duas médias móveis ponderadas (WMA 6 e WMA 85 no preço típico) definem a tendência imediata. Operações compradas requerem que a WMA rápida esteja acima da WMA lenta, vendidas o contrário.
3. **Confirmação de momentum**
   - Um indicador de Momentum de período superior (comprimento 14) verifica se o preço se desvia suficientemente do nível 100. O desvio máximo dos últimos três valores do período superior deve exceder o limiar específico de direção.
   - O período superior é selecionado automaticamente para corresponder ao mapeamento usado no script MT4 (ex., M15 → H1, H1 → D1, D1 → mensal). Os dados semanais também recorrem à confirmação mensal. Se nenhum período superior estiver disponível, o filtro de momentum é ignorado.
4. **Filtro macro**
   - Um MACD mensal (12/26/9) garante que o momentum de longo prazo corresponde à direção da operação (linha MACD acima do sinal para comprados, abaixo para vendidos).
5. **Regras de entrada**
   - Comprados: expansão de bandas, WMA rápida acima da WMA lenta, MACD mensal altista, desvio de momentum do período superior acima de `MomentumBuyThreshold`, e sobreposição estrutural de candles (`candle[-2].Low < candle[-1].High`).
   - Vendidos: expansão de bandas, WMA rápida abaixo da WMA lenta, MACD mensal baixista, desvio de momentum acima de `MomentumSellThreshold`, e a condição de candle espelhada (`candle[-1].Low < candle[-2].High`).
6. **Regras de saída**
   - As posições são fechadas quando o preço fecha em ou além da Bollinger Band exterior na direção da operação (ou seja, comprados saem na banda superior, vendidos na banda inferior), refletindo a implementação MT4.
   - `StartProtection()` habilita a infraestrutura de ordens protetoras do StockSharp para que extensões de stop-loss/take-profit possam ser adicionadas se necessário.

## Indicadores e assinaturas de dados
- Candles do período primário definidos por `CandleType`.
- Candles do período superior para confirmação de momentum (mapeados automaticamente do período base).
- Candles mensais para filtragem MACD (aproximação de 30 dias).
- Indicadores: Bollinger Bands, duas Médias Móveis Ponderadas (preço típico), Momentum e MovingAverageConvergenceDivergenceSignal.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Candles de 15 minutos | Período de trabalho primário. |
| `BollingerPeriod` | 20 | Comprimento da Bollinger Band. |
| `BollingerWidth` | 2.0 | Multiplicador de desvio padrão da Bollinger Band. |
| `SqueezeRatio` | 1.1 | Razão mínima de expansão de largura entre bandas atuais e históricas. |
| `RetraceCandles` | 10 | Retrospectiva usada para comparação de squeeze. |
| `FastMaLength` | 6 | Comprimento da WMA rápida (preço típico). |
| `SlowMaLength` | 85 | Comprimento da WMA lenta (preço típico). |
| `MomentumLength` | 14 | Período de Momentum no período superior. |
| `MomentumBuyThreshold` | 0.3 | Desvio mínimo de 100 necessário para validar entradas compradas. |
| `MomentumSellThreshold` | 0.3 | Desvio mínimo de 100 necessário para validar entradas vendidas. |

Todos os parâmetros são expostos como valores `StrategyParam<T>` e podem ser otimizados dentro do StockSharp Designer ou em tempo de execução.

## Notas de implementação
- A estratégia usa `SubscribeCandles().BindEx(...)` para manter o cabeamento do indicador declarativo e evita coleções de indicadores manuais, conforme exigido pelas diretrizes da API de alto nível.
- As médias móveis ponderadas são impulsionadas pelo preço típico dentro do callback de processamento de candles para preservar o comportamento dos cálculos LWMA no script MT4.
- Os valores de momentum do período superior são armazenados em uma fila de três elementos para imitar os retornos 1–3 de `iMomentum` do código original.
- Os valores MACD mensais persistem em campos de classe para que cada candle do período primário tenha acesso ao viés de longo prazo mais recente.
- As saídas acionadas pelas bandas exteriores substituem os blocos de trailing stop/break-even MT4 enquanto retêm a intenção visual de fechar quando o preço toca o envelope oposto.
- A estratégia deixa o dimensionamento de ordens para a base `Strategy.Volume`. Os giros de posição automaticamente compensam qualquer exposição existente adicionando `Math.Abs(Position)` ao volume da ordem.
