# Estratégia de MACD Power
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia MACD Power é um sistema de momentum multitemporal convertido do expert advisor MetaTrader original. A lógica combina um par de médias móveis linearmente ponderadas (LWMA) calculadas no período principal, duas variações de MACD, um filtro de momentum em um período superior e um viés MACD mensal. A estratégia tenta participar de movimentos impulsivos quando o momentum e as condições de tendência do período superior se alinham.

## Lógica principal
- **Médias móveis primárias** – Uma LWMA rápida e uma lenta do preço típico da vela (\((High + Low + Close) / 3\)). A estratégia exige que a média rápida esteja abaixo da lenta antes de considerar qualquer sinal, espelhando o código original que aguarda retrações dentro de uma inclinação bajista dominante antes de entrar na direção do viés mensal.
- **Confirmação dupla de MACD** – Dois indicadores MACD com parâmetros `(12, 26, 1)` e `(6, 13, 1)` devem estar ambos acima de zero para operações compradas ou abaixo de zero para vendidas. Esses valores reproduzem as condições `MacdMAIN1` e `MacdMAIN2` do expert MQL que medem aceleração de curto prazo.
- **Filtro de momentum** – O Momentum (comprimento 14) é calculado em um período superior derivado do tamanho da vela principal (ex.: base de 15 minutos → momentum de 1 hora). A distância absoluta de 100 é monitorada nas três últimas leituras de momentum; pelo menos uma delas deve exceder o limiar configurado para confirmar que o preço está se movendo decisivamente.
- **Viés MACD mensal** – Um MACD mensal `(12, 26, 9)` (idêntico a `MacdMAIN0`/`MacdSIGNAL0` no EA) deve ter sua linha principal acima da linha de sinal para operações compradas e abaixo para vendidas. Isso protege contra operar contra a tendência macro dominante.

## Gestão de operações
- **Dimensionamento de entrada** – O parâmetro `OrderVolume` define o tamanho base da ordem. Quando uma reversão de posição é necessária, o motor adiciona automaticamente a magnitude da posição oposta para que o volume líquido seja invertido em uma única ordem a mercado.
- **Take profit / stop loss** – Distâncias absolutas são expressas em pontos do instrumento e convertidas em preço usando `Security.PriceStep` (com fallback seguro para `1`).
- **Trailing stop** – Uma vez que o preço se move a favor em `TrailingActivationPoints`, o stop rastreia o preço mais alto (comprado) ou mais baixo (vendido) com um offset definido por `TrailingOffsetPoints`.
- **Ponto de equilíbrio** – Quando o preço atinge `BreakEvenTriggerPoints`, um stop de ponto de equilíbrio sintético é ativado em `Entrada ± BreakEvenOffsetPoints`. Se o preço recuar para esse nível, a posição é fechada.
- **Limite de operações** – `MaxTrades` limita o número de iniciações de posição por execução; uma vez atingido o limiar, nenhuma nova entrada é emitida.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Período principal para geração de sinais. | Velas de 15 minutos |
| `FastMaLength` | Comprimento da LWMA rápida (preço típico). | 6 |
| `SlowMaLength` | Comprimento da LWMA lenta (preço típico). | 85 |
| `MomentumLength` | Lookback do Momentum no período superior. | 14 |
| `MomentumBuyThreshold` | Distância mínima absoluta de 100 necessária para momentum de alta. | 0.3 |
| `MomentumSellThreshold` | Distância mínima absoluta de 100 necessária para momentum de baixa. | 0.3 |
| `TakeProfitPoints` | Distância do take-profit em pontos do instrumento. | 50 |
| `StopLossPoints` | Distância do stop-loss em pontos do instrumento. | 20 |
| `TrailingActivationPoints` | Lucro (pontos) necessário antes de o trailing ativar. | 40 |
| `TrailingOffsetPoints` | Distância (pontos) entre o trailing stop e o preço extremo. | 40 |
| `BreakEvenTriggerPoints` | Lucro (pontos) que ativa a proteção de ponto de equilíbrio. | 30 |
| `BreakEvenOffsetPoints` | Offset (pontos) aplicado ao mover o stop para o ponto de equilíbrio. | 30 |
| `MaxTrades` | Número máximo de operações permitidas por sessão. | 10 |
| `OrderVolume` | Volume base da ordem. | 1 |

## Diferenças em relação ao expert MQL
- A estratégia usa a API de alto nível do StockSharp (`SubscribeCandles` + `Bind/BindEx`) em vez de polling direto de ticks. Os valores dos indicadores são processados apenas após o fechamento das velas.
- Blocos de trailing baseado em dinheiro e stop de capital do código original não são portados porque o gerenciamento de dinheiro no nível de conta normalmente é tratado pelo framework de risco do StockSharp. Em vez disso, trailing e ponto de equilíbrio baseados em pontos permanecem e podem ser configurados para emular o comportamento do EA.
- Alertas, notificações e auxiliares de modificação manual de ordens do MQL são omitidos; o motor StockSharp gerencia ordens diretamente via chamadas a mercado.

## Notas de uso
1. Escolha o período principal configurando `CandleType`. O momentum do período superior e o MACD mensal são derivados automaticamente conforme o mapeamento implementado em `GetMomentumCandleType()`.
2. Alinhe `TakeProfitPoints`, `StopLossPoints` e os parâmetros de trailing/ponto de equilíbrio com o tamanho de tick do instrumento. Os padrões refletem as configurações Forex de 5 dígitos do EA, mas podem ser adaptados para outros mercados.
3. Monitore o contador `MaxTrades` ao executar backtests automatizados; defina-o como um número grande se o comportamento de empilhamento tipo martingale do EA original for desejado.
4. Para análise visual, ative gráficos na GUI – a implementação desenha velas e as duas curvas LWMA por padrão.
