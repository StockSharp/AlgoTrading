# Trader FX incrível
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz a configuração MetaTrader de `MQL/8539`, que consiste nos indicadores personalizados **AwesomeFxTradera.mq4** e **t_ma.mq4**. O código original pinta o histograma Bill Williams Awesome Oscillator em verde ou vermelho, dependendo se o valor está subindo ou descendo, e sobrepõe uma média móvel ponderada linear de 34 períodos (LWMA) ao lado de um clone suavizado da mesma curva. A porta StockSharp mantém os mesmos cálculos e converte as cores dos indicadores em sinais de negociação.

## Lógica MQL original

1. **AwesomeFxTradera.mq4** calcula duas médias móveis exponenciais aplicadas ao **preço de abertura** com os períodos 8 e 13. Sua diferença é armazenada em `ExtBuffer0`. O buffer é pintado de verde quando o valor atual é maior que a barra anterior e de vermelho quando é menor. Isto codifica efetivamente a direção do momento, não apenas o seu sinal.
2. **t_ma.mq4** traça um LWMA de 34 períodos do preço de abertura (`ExtMapBuffer1`) e uma média móvel simples de 6 períodos desse LWMA (`ExtMapBuffer2`). O mais suave monitora se a média da tendência acelera ou desacelera.

O gráfico MetaTrader, portanto, destaca o impulso de alta quando o oscilador está acima de zero e continua aumentando enquanto o preço é negociado acima do LWMA suavizado. O momentum de baixa é a configuração oposta.

## StockSharp implementação

O `AwesomeFxTraderStrategy` assina um tipo de vela configurável (padrão **M15**) e alimenta os indicadores com o preço de abertura da vela para corresponder aos buffers MetaTrader.

1. As EMAs rápidas e lentas são recalculadas em cada vela finalizada; sua diferença reproduz o histograma oscilante.
2. O LWMA rastreia a tendência de 34 barras e um SMA de 6 barras a suaviza. A comparação das duas séries revela se a curva de tendência está subindo ou descendo.
3. A cor do oscilador é reconstruída comparando o valor atual do histograma com a barra anterior, seguindo a lógica `bool up` da implementação MQL.
4. **Regras de entrada**:
   - Insira longo quando o oscilador estiver positivo, subindo (buffer verde) e o LWMA estiver acima do seu nível mais suave.
   - Entre em posição curta quando o oscilador estiver negativo, caindo (buffer vermelho) e o LWMA estiver abaixo do seu nível mais suave.
5. **Regras de saída/reversão**: um sinal oposto inverte a posição. O tamanho da ordem é aumentado automaticamente pela posição atual absoluta, de modo que as posições vendidas sejam fechadas antes que uma posição comprada seja estabelecida e vice-versa.

Nenhum nível extra de stop-loss ou take-profit é definido no código-fonte, portanto, a porta depende apenas de mudanças de impulso para saídas. As declarações de registro documentam cada gatilho de negociação junto com as leituras dos indicadores.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `FastEmaPeriod` | 8 | Comprimento do EMA rápido usado na réplica do oscilador. |
| `SlowEmaPeriod` | 13 | Comprimento da lentidão EMA. |
| `TrendLwmaPeriod` | 34 | Período do filtro de tendência LWMA retirado de `t_ma.mq4`. |
| `TrendSmoothingPeriod` | 6 | Janela do SMA aplicada aos valores LWMA. |
| `CandleType` | Período de 15 minutos | Tipo de dados Candle usado para cálculos de impulso e tendência. |

Todos os parâmetros podem ser otimizados por meio da IU do StockSharp graças aos metadados do `StrategyParam`.

## Mapeamento de arquivos

| arquivo MetaTrader | StockSharp contraparte | Notas |
| --- | --- | --- |
| `MQL/8539/AwesomeFxTradera.mq4` | `CS/AwesomeFxTraderStrategy.cs` | Recria o oscilador EMA-on-open e sua lógica de cores ascendente/descendente. |
| `MQL/8539/t_ma.mq4` | `CS/AwesomeFxTraderStrategy.cs` | Implementa o LWMA de 34 períodos com um SMA mais suave de 6 períodos para detecção de tendências. |

A versão Python foi omitida intencionalmente conforme solicitado.
