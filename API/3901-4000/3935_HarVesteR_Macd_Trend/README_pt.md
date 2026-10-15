# Estratégia de tendências HarVesteR MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia HarVesteR é um sistema de acompanhamento de tendências convertido do consultor MetaTrader original. Ele combina a confirmação de impulso MACD com duas médias móveis simples que definem a direção da tendência e gerenciam as saídas finais. Um filtro ADX opcional mantém a atividade de negociação focada em movimentos direcionais fortes.

A configuração padrão reflete o Expert Advisor publicado: MACD(12, 24, 9), um gerenciamento de 50 períodos SMA, um filtro de tendência de 100 períodos SMA e um take-profit encenado que divide a posição pela metade quando o preço viaja duas vezes o risco inicial.

## Lógica de negociação
1. **Viés de tendência** – O SMA de 100 períodos atua como uma porta direcional. O fechamento do preço abaixo dele arma a configuração longa, enquanto o fechamento acima dele arma a configuração curta. Assim que uma negociação é realizada, a bandeira é redefinida até que o preço volte para o lado oposto, evitando entradas consecutivas sem retrocesso.
2. **MACD confirmação** – Um sinal é válido somente se a linha MACD estiver no lado esperado de zero e estiver no lado oposto pelo menos uma vez nas últimas *Barras de Confirmação*. Isso replica o loop original que buscava uma mudança de sinal dentro de uma janela deslizante.
3. **Condições de entrada** – As negociações longas exigem que o fechamento da vela mais o deslocamento configurado (em pontos de preço) estejam acima de ambos os SMAs, MACD sejam positivos e (se habilitado) ADX excedam 50. As negociações curtas usam a lógica de espelho com MACD negativo e preço abaixo de ambos os SMAs.
4. **Stop inicial** – O stop-loss é ancorado no preço mais baixo (para posições compradas) ou mais alto (para posições vendidas) das últimas *Stop Bars* velas concluídas, correspondendo às chamadas MQL `iLowest`/`iHighest` com um deslocamento de uma barra.
5. **Gerenciamento de posição** – Quando o preço percorre uma distância igual ao *Multiplicador de risco* vezes o risco inicial, metade da posição é fechada e o stop é movido para o ponto de equilíbrio. A metade restante sai quando o preço recua o suficiente para que o período SMA de 50 períodos cruze acima (longo) ou abaixo (curto) do fechamento ajustado pela compensação.
6. **Saída protetora** – Qualquer vela que rompa o preço stop armazenado fecha imediatamente toda a posição.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `Fast EMA` | Período curto EMA usado dentro do cálculo MACD. | 12 |
| `Slow EMA` | Longo período EMA usado dentro do cálculo MACD. | 24 |
| `Signal EMA` | Período de suavização para a linha de sinal MACD. | 9 |
| `MACD Confirmation Bars` | Máximo de velas entre leituras opostas de MACD necessárias antes de uma nova entrada. | 6 |
| `Trend SMA` | Comprimento do gerenciamento SMA que protege as saídas finais. | 50 |
| `Filter SMA` | Comprimento do direcional SMA usado para armar configurações longas/curtas. | 100 |
| `Offset (points)` | Compensação (em pontos do instrumento) adicionada ou subtraída ao comparar o preço com os SMAs. | 10 |
| `Stop Bars` | Número de velas passadas consideradas ao definir o stop inicial. | 6 |
| `Risk Multiplier` | Multiplicador aplicado à distância de risco inicial para acionar o take-profit parcial. | 2,0 |
| `Use ADX` | Ativa o filtro de intensidade de tendência ADX>50. | Desativado |
| `ADX Period` | ADX lookback usado quando o filtro está ativo. | 14 |
| `Candle Type` | Série de velas fornecidas aos indicadores (o padrão é barras de 1 hora). | Período de 1h |

## Notas de implementação
- As compensações de preços são convertidas em preços absolutos via `Security.Step` (ou `Security.PriceStep` quando disponível). Se o título não expor uma etapa, a estratégia volta para `0.0001`, correspondendo ao comportamento do consultor original focado em FX.
- As saídas parciais utilizam ordens de mercado dimensionadas para metade da posição atual, refletindo a redução de lote realizada na implementação de origem MQL.
- `StartProtection()` está habilitado para garantir que o protetor de posição integrado esteja ativo antes que novas negociações sejam feitas.
- O filtro ADX é opcional; quando desativado, o algoritmo se comporta exatamente como o script histórico, substituindo ADX por um valor artificial de 60.

## Dicas de uso
1. Configure a propriedade `Volume` antes de iniciar a estratégia; define o tamanho base do pedido utilizado durante entradas e saídas parciais.
2. Alinhe o `Candle Type` com seu período preferido. A estratégia original foi ajustada em dados horários, mas quadros mais curtos podem ser explorados através da otimização de parâmetros.
3. A otimização de `MACD Confirmation Bars`, `Offset (points)` e `Risk Multiplier` normalmente tem o maior impacto na taxa de ganhos e na frequência de negociação.
