# Macd Pattern Trader Todos v0.01
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica o consultor especialista "MacdPatternTraderAll v0.01" MetaTrader. Ele executa seis padrões de entrada independentes baseados em MACD no mesmo fluxo de vela, gerencia o risco com níveis adaptativos de stop-loss e take-profit, realiza realização de lucros encenada e, opcionalmente, aplica uma regra de dimensionamento de martingale lento após perder ciclos.

## Recursos principais

- **Seis configurações de MACD** – cada padrão usa seus próprios períodos EMA rápidos/lentos e níveis de limite (`Pattern1`… `Pattern6`). Os padrões podem ser ativados ou desativados de forma independente.
- **Níveis de risco dinâmicos** – os níveis de stop-loss são derivados de máximos/mínimos recentes com compensações configuráveis, enquanto os níveis de take-profit iteram em blocos de barras sucessivos para espelhar a implementação original do MQL.
- **Filtro de sessão** – a estratégia é negociada apenas dentro da janela configurável `StartTime` / `StopTime` quando `UseTimeFilter` está ativado.
- **Saídas parciais** – as posições lucrativas são ampliadas em duas etapas, uma vez que os filtros EMA/SMA confirmam o impulso, seguindo a lógica original `ActivePosManager`.
- **Martingale lento** – quando `UseMartingale` é verdadeiro, o tamanho da próxima negociação dobra após um ciclo de perdas e é reiniciado após qualquer ciclo lucrativo.

## Lógica de entrada por padrão

1. **Padrão 1 (tag `Pattern1`)**
   - Arma um pouco depois que a linha principal MACD passa acima de `Pattern1MaxThreshold` e então rola com uma sequência mais baixa e alta.
   - Braços muito depois de alongar abaixo de `Pattern1MinThreshold` e produzir uma sequência grave mais alta.
2. **Padrão 2 (tag `Pattern2`)**
   - Conta oscilações em torno da linha zero. Shorts são acionados quando uma oscilação positiva falha perto de `Pattern2MinThreshold`. As compras aparecem quando uma oscilação negativa desaparece perto de `Pattern2MaxThreshold`. O algoritmo reproduz as verificações de distância originais comparando valores MACD absolutos (`valueMin2` / `valueCurr2`).
3. **Padrão 3 (tag `Pattern3`)**
   - Rastreia até três topos descendentes (ou ascendentes) MACD para detectar um "gancho triplo". Somente quando todos os limites intermediários (`Pattern3MaxThreshold`, `Pattern3MaxLowThreshold`, `Pattern3MinThreshold`, `Pattern3MinHighThreshold`) concordarem é que novas posições serão permitidas.
4. **Padrão 4 (tag `Pattern4`)**
   - Observa MACD picos fora de `Pattern4MaxThreshold` / `Pattern4MinThreshold` seguidos por tentativas fracassadas de atingir novos extremos. Um contador extra (`Pattern4AdditionalBars`) é preservado para compatibilidade.
5. **Padrão 5 (tag `Pattern5`)**
   - Implementa o rompimento da zona neutra usado no consultor especialista. Os shorts exigem um rebote abaixo de `Pattern5MinThreshold` para dentro da zona neutra e outra falha. Os longos seguem a sequência espelhada em torno de `Pattern5MaxThreshold`.
6. **Padrão 6 (tag `Pattern6`)**
   - Conta o número de barras consecutivas acima/abaixo dos níveis limite. Depois de gastar mais de `Pattern6TriggerBars` dentro da área de sobrecompra/sobrevenda e retornar abaixo/acima do limite, a estratégia abre uma negociação, a menos que `Pattern6MaxBars` bloqueie o sinal.

Cada padrão usa os métodos auxiliares `TryOpenLong` / `TryOpenShort`, garantindo que as paradas e as metas sejam calculadas antes de qualquer pedido ser emitido.

## Gestão de Risco e Comércio

- **Stop-loss**: `CalculateStopPrice` verifica as velas concluídas `stopBars` mais recentes (excluindo a ativa) e aplica o ponto configurado `offset`. Os preços são ajustados para instrumentos decimais de 3/5, assim como na versão MQL.
- **Take-profit**: `CalculateTakeProfit` percorre blocos consecutivos de `takeBars` velas até que nenhum novo extremo seja encontrado, imitando o loop `iLowest` / `iHighest` aninhado do código original.
- **Saídas parciais**: `ManageActivePositions` fecha um terço da posição com lucro de `ProfitThreshold` quando o preço é confirmado com `ema2`. Uma segunda saída com metade do tamanho é acionada quando o preço atinge o filtro `(sma3 + ema4) / 2` combinado.
- **Saídas difíceis**: `CheckRiskManagement` emite saídas completas do mercado assim que os níveis armazenados de stop-loss ou take-profit são atingidos.
- **Martingale controle**: `OnOwnTradeReceived` acumula PnL realizado para o ciclo constante a estável atual. Quando a posição retorna ao nível estável, `AdjustVolumeOnFlat` redefine o volume para `InitialVolume` após os lucros ou o dobra após as perdas se `UseMartingale` estiver ativado.

## Parâmetros

Todos os botões de configuração são expostos por meio de propriedades `StrategyParam<T>` para otimização no StockSharp Designer.

- **Geral**: `CandleType`, `InitialVolume`, `UseTimeFilter`, `StartTime`, `StopTime`, `UseMartingale`.
- **Padrões 1–6**: contagens de barras de stop-loss/take-profit, compensações, MACD períodos rápidos/lentos e níveis de limite correspondentes às entradas externas do script MQL.
- **Gerenciador de posição**: comprimentos EMA/SMA (`EmaPeriod1`, `EmaPeriod2`, `SmaPeriod3`, `EmaPeriod4`) usados no filtro de saída parcial.

Todos os valores padrão espelham as variáveis `extern` de `MacdPatternTraderAll v0.01`.

## Notas de uso

- A estratégia espera um símbolo com `PriceStep` e `Decimals` válidos para calcular os deslocamentos corretamente.
- Forneça uma série de velas por meio de `CandleType` (por exemplo, `TimeSpan.FromMinutes(5).TimeFrame()`).
- Quando vários padrões são acionados simultaneamente, a estratégia abrirá apenas uma posição porque cada chamada de entrada recalcula o volume combinado desejado e limpa os stops opostos.
- A lógica de saída faseada funciona com posições agregadas, pelo que os fechos parciais ocorrem mesmo que vários padrões partilhem a mesma direção de negociação.
