# Estratégia de tendência de topos e fundos RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão StockSharp do consultor especialista MetaTrader "Tops bottoms trend and rsi ea". Ele monitora velas concluídas do período selecionado, procura topos ou fundos de tendências emergentes dentro de uma janela de lookback configurável e confirma cada oportunidade com um filtro de Índice de Força Relativa (RSI). Quando os critérios são atendidos, a estratégia abre uma ordem de mercado única e atribui imediatamente níveis protetores de stop-loss e take-profit derivados de distâncias baseadas em pip.

## Lógica de negociação
- **Fonte de dados** – o algoritmo se inscreve no tipo de vela configurado e avalia apenas velas finalizadas para evitar o uso de dados incompletos.
- **Detecção de fundo (configuração longa)** – o fechamento da última vela deve estar pelo menos `BuyTrendPips` pips abaixo da máxima da vela há `BuyTrendCandles` barras. Todos os mínimos intermediários devem permanecer acima do fechamento atual, e o filtro de qualidade (`BuyTrendQuality`) exige que os máximos recentes não se desviem muito do máximo de referência. Quando esta estrutura se forma e o valor RSI da vela anterior está abaixo de `BuyRsiThreshold`, a estratégia abre uma posição longa com volume `BuyVolume`.
- **Detecção de topo (configuração curta)** – o fechamento da última vela deve estar pelo menos `SellTrendPips` pips acima da mínima da vela há `SellTrendCandles` barras. Os máximos intermediários devem permanecer abaixo do fechamento atual, enquanto o filtro de qualidade (`SellTrendQuality`) mantém os mínimos recentes próximos ao mínimo de referência. Se o valor RSI da vela anterior exceder `SellRsiThreshold`, a estratégia abre uma posição curta com volume `SellVolume`.
- **Gerenciamento de risco** – após cada entrada, a estratégia armazena o preço de preenchimento e calcula os níveis de proteção baseados em pip. As compensações de stop-loss usam `BuyStopLossPips` ou `SellStopLossPips`. As distâncias de lucro são derivadas principalmente do stop via `BuyTakeProfitPercentOfStop` ou `SellTakeProfitPercentOfStop`. Se a porcentagem de longo take-profit estiver desativada (`0`), a distância fixa `BuyTakeProfitPips` será usada. Sempre que as velas subsequentes tocam os níveis correspondentes de stop ou take-profit, a posição é fechada com uma ordem de mercado.
- **Controle de posição** – o sistema mantém no máximo uma posição aberta. Novos sinais são ignorados enquanto existe uma posição ou ordem ativa. A confirmação de RSI sempre depende da vela anterior (mudança de uma barra), espelhando o EA original.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `BuyVolume` | Volume de pedidos usado para posições longas. | `0.01` |
| `BuyStopLossPips` | Distância de stop-loss para negociações longas em pips. | `20` |
| `BuyTakeProfitPips` | Corrigida a distância de take-profit em pips para posições compradas quando o modo percentual está desativado. | `5` |
| `BuyTakeProfitPercentOfStop` | Take-profit como uma porcentagem da longa distância stop-loss. | `100` |
| `SellVolume` | Volume de pedidos usado para posições curtas. | `0.01` |
| `SellStopLossPips` | Distância de stop-loss para negociações curtas em pips. | `20` |
| `SellTakeProfitPercentOfStop` | Take-profit como uma porcentagem da distância curta do stop-loss. | `100` |
| `SellTrendCandles` | Número de velas inspecionadas na busca por novos topos. | `10` |
| `SellTrendPips` | Avanço mínimo acima do mínimo de referência necessário para um setup curto (pips). | `20` |
| `SellTrendQuality` | Filtro de qualidade de tendência para configurações curtas (fixado na faixa de 1 a 9). | `5` |
| `BuyTrendCandles` | Número de velas inspecionadas na busca por novos fundos. | `10` |
| `BuyTrendPips` | Declínio mínimo abaixo do máximo de referência necessário para uma configuração longa (pips). | `20` |
| `BuyTrendQuality` | Filtro de qualidade de tendência para configurações longas (fixado na faixa de 1 a 9). | `5` |
| `BuyRsiPeriod` | RSI período usado para confirmações longas. | `14` |
| `BuyRsiThreshold` | RSI limite de sobrevenda que deve ser ultrapassado de cima para permitir entradas longas. | `40` |
| `SellRsiPeriod` | RSI período usado para confirmações curtas. | `14` |
| `SellRsiThreshold` | RSI limite de sobrecompra que deve ser ultrapassado abaixo para permitir entradas curtas. | `60` |
| `CandleType` | Prazo das velas processadas pela estratégia. | `30-minute time frame` |

## Notas
- As distâncias pip são convertidas em preços usando o `PriceStep` do título. As cotações forex de cinco dígitos e pip fracionário são normalizadas para o tamanho clássico do pip, replicando as regras de conversão do EA original.
- Como a confirmação RSI usa a vela anterior (shift = 1), a estratégia precisa de pelo menos um valor RSI totalmente formado antes de poder negociar. As primeiras velas após a inicialização são, portanto, ignoradas.
- A lógica cancela todos os níveis de proteção sempre que uma posição é totalmente fechada, garantindo que a próxima entrada comece com novos parâmetros de risco.
