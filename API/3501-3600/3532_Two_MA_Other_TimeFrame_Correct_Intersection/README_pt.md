# Dois MA Outra Estratégia de Intersecção Correta de Prazo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma porta StockSharp do consultor especialista MetaTrader 5 "Two MA Other TimeFrame Correct Intersection". O EA original depende de duas médias móveis, cada uma calculada em seu próprio período de tempo (por exemplo, H1 vs D1), enquanto as decisões de negociação são sincronizadas com o período do gráfico. A conversão mantém o comportamento multi-timeframe e abre posições longas quando a média móvel rápida cruza acima da média móvel lenta. Por outro lado, as posições curtas são abertas quando a média rápida cruza abaixo da lenta. Todas as ordens são executadas ao preço de mercado e a estratégia sempre fecha qualquer exposição oposta antes de abrir uma nova negociação, correspondendo ao modelo de execução orientado pelo mecanismo do script MQL5.

## Lógica de negociação
- Assine três fluxos de velas: o período de negociação principal, o período de MA rápida e o período de MA lenta.
- Calcule as médias móveis rápidas e lentas em seus intervalos de tempo dedicados. Cada média móvel suporta os mesmos métodos de suavização e fontes de preços que foram expostos pelo indicador `iCustom` original.
- Opcionalmente, aplique um deslocamento horizontal configurável às saídas da média móvel antes de serem comparadas, reproduzindo as entradas `ma_shift` do EA.
- Cada vez que uma vela no período de negociação primário termina, verifique se há um cruzamento entre os valores da média móvel mais recente e anterior:
  - Se a MM rápida estava abaixo da MM lenta na etapa anterior e agora está acima dela, feche qualquer posição curta e abra (ou inverta) uma posição longa.
  - Se a MM rápida estava acima da MM lenta na etapa anterior e agora está abaixo dela, feche qualquer posição longa e abra (ou inverta) uma posição curta.
- Todas as entradas usam o volume de negociação configurado. Ao reverter uma posição existente, a estratégia aumenta o tamanho da ordem pela magnitude da exposição oposta para garantir que a posição seja invertida numa única ordem de mercado.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradeVolume` | Volume base para entradas no mercado. Usado para negociações longas e curtas. |
| `CandleType` | Prazo de negociação primário. Os sinais são avaliados sempre que uma vela deste tipo fecha. |
| `FastTimeFrame` | Prazo usado para construir a média móvel rápida. |
| `SlowTimeFrame` | Prazo usado para construir a média móvel lenta. |
| `FastLength` | Número de barras incluídas na média móvel rápida. |
| `SlowLength` | Número de barras incluídas na média móvel lenta. |
| `FastShift` | Deslocamento horizontal aplicado à média móvel rápida antes da comparação. |
| `SlowShift` | Deslocamento horizontal aplicado à produção média móvel lenta antes da comparação. |
| `FastMethod` | Algoritmo de suavização para média móvel rápida (simples, exponencial, suavizada ou linear ponderada). |
| `SlowMethod` | Algoritmo de suavização para a média móvel lenta. |
| `FastAppliedPrice` | Preço da vela utilizado pela média móvel rápida (abertura, máxima, mínima, fechamento, mediana, típica ou ponderada). |
| `SlowAppliedPrice` | Preço da vela usado pela média móvel lenta. |

## Notas de implementação
- As médias móveis são processadas por meio de StockSharp assinaturas de alto nível (`SubscribeCandles().Bind(...)`) e continuam em execução mesmo quando o período de negociação difere do período de cálculo.
- Os parâmetros de deslocamento são implementados com pequenas filas que atrasam a saída do indicador pelo número solicitado de barras, replicando o comportamento das entradas `ma_shift`.
- A estratégia usa `StartProtection()` para se alinhar com StockSharp utilitários de proteção de conta, assim como o mecanismo de negociação original que protegia as posições abertas.
- A renderização do gráfico adiciona as velas primárias às médias móveis rápidas e lentas para que os sinais de cruzamento permaneçam visíveis durante os backtests.
- Não há módulo de stop-loss, take-profit ou trailing-stop no EA original. Os traders podem combinar este módulo com estratégias separadas de gestão de dinheiro se for necessário um controlo de risco adicional.
