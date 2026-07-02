# Estratégia de Surf 3.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia C# é uma versão fiel do MetaTrader 4 expert **Surfing 3.0**. Ele recria a lógica de rompimento que observa um envelope de média móvel exponencial (EMA) construído a partir dos máximos e mínimos das velas. Sempre que a barra anterior fecha dentro da banda e a última barra fechada a perfura, o sistema reage com uma negociação direcional. A tradução depende do alto nível API de API, assinaturas de velas e indicadores integrados em vez de buffers escritos à mão.

O algoritmo funciona exclusivamente com velas prontas de uma agregação configurável. Ele mantém apenas a quantidade mínima de estado necessária para emular os lookbacks `iMA` e `iClose` usados ​​pelo código original. Cada decisão é tomada uma vez por barra fechada, correspondendo ao estilo de avaliação "barra fechada" da implementação MQL.

## Indicadores

- **Alta EMA / Mínima EMA** – Duas médias móveis exponenciais calculadas nas máximas e mínimas das velas. Eles formam um envelope dinâmico que define níveis de ruptura para entradas longas e curtas.
- **Índice de Força Relativa (RSI)** – Atua como um filtro de tendência. As posições longas exigem que RSI esteja acima de `LongRsiThreshold`, enquanto as posições curtas são permitidas apenas quando estiver abaixo de `ShortRsiThreshold`.

## Lógica de negociação

1. Assine velas do tipo `CandleType` e atualize os indicadores EMA e RSI para cada barra finalizada.
2. Armazene os valores da barra fechada anterior do preço de fechamento e dos EMA máximos/mínimos. Estes representam `PriceClose_2`, `PriceHigh_2` e `PriceLow_2` do especialista original.
3. Quando a última barra fechada (`PriceClose_1`) cruza **acima** da máxima EMA enquanto o fechamento anterior estava abaixo ou igual a ela e o filtro RSI confirma:
   - Feche qualquer posição curta aberta.
   - Abra uma ordem longa de mercado com volume `OrderVolume`.
   - Calcule as compensações de stop loss e take-profit em pontos de instrumento.
4. Quando a última barra fechada cruza **abaixo** do mínimo EMA enquanto o fechamento anterior estava acima ou igual a ele e o RSI está abaixo do limite curto:
   - Feche qualquer posição longa aberta.
   - Abra uma ordem curta de mercado com volume `OrderVolume`.
   - Aplique os níveis de proteção usando as mesmas distâncias baseadas em pontos.
5. Apenas uma posição líquida pode estar ativa. Os sinais de reversão sempre nivelam a exposição existente antes de entrar na direção oposta.
6. Fora da janela de negociação `[TradeStartHour, TradeEndHour)`, nenhuma nova negociação é iniciada. Quando o relógio atinge `TradeEndHour`, a estratégia fecha qualquer posição restante e zera seu histórico interno, imitando a chamada `closeAllPos()` na versão MQL.

## Gestão de risco

- **Stop Loss / Take Profit** – Expresso em pontos de instrumento e convertido usando a etapa de preço do título. Ambos são opcionais; definir uma distância de `0` desativa o respectivo nível.
- **Sessão fixa** – No final da janela de negociação permitida, todas as posições abertas são fechadas no mercado e o rastreamento de stop/takeprofit é apagado. Isso evita que as posições mudem durante a noite, exatamente como o especialista original aplicou com `startHour` / `endHour`.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-------------|---------|
| `OrderVolume` | Volume de negociação usado para cada ordem de mercado. | `1` |
| `TakeProfitPoints` | Distância Take Profit expressa em pontos de instrumento. | `80` |
| `StopLossPoints` | Distância de stop loss expressa em pontos do instrumento. | `50` |
| `MaPeriod` | Comprimento do EMA aplicado a altos e baixos. | `50` |
| `RsiPeriod` | Período do filtro RSI. | `10` |
| `LongRsiThreshold` | Valor mínimo de RSI necessário para permitir entradas longas. | `40` |
| `ShortRsiThreshold` | Valor máximo de RSI permitido para entrar em posições curtas. | `65` |
| `TradeStartHour` | Hora (horário de câmbio) a partir da qual novas negociações são permitidas. | `8` |
| `TradeEndHour` | Hora (exclusiva) após a qual as posições são fechadas e nenhuma nova negociação é iniciada. | `18` |
| `CandleType` | Agregação de velas usada para todos os cálculos (padrão: velas de 15 minutos). | `15m` |

## Notas

- Os sinais são avaliados estritamente nas velas finalizadas; flutuações intrabarras são ignoradas como em MetaTrader.
- A estratégia redefine seu histórico EMA quando a sessão de negociação termina para evitar misturar dados de dias diferentes.
- A tradução do Python é omitida intencionalmente de acordo com as diretrizes do projeto.
