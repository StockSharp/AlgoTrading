# MACD Exemplo de estratégia não tão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia MACD Not So Sample é uma conversão do consultor especialista MetaTrader *MACD_Not_So_Sample*. O robô original negocia
um gráfico EURUSD de 4 horas usando cruzamentos MACD confirmados por um filtro de tendência EMA, combinado com grandes níveis de lucro e um
parada final. A versão StockSharp mantém a mesma estrutura: o histograma MACD deve ser negativo e cruzar acima de seu sinal
linha para uma entrada longa, enquanto um histograma positivo cruzando abaixo do sinal produz uma entrada curta. Uma tendência EMA deve confirmar a
direção antes de qualquer posição ser aberta.

Todos os recursos de gerenciamento de dinheiro são implementados em StockSharp: a estratégia define uma meta de lucro configurável, gerencia um
trailing stop quando o preço viaja longe o suficiente e fecha as negociações quando o MACD cruza na direção oposta com suficiente
força. A porta usa indicadores StockSharp e assinaturas de velas de alto nível para que todos os cálculos aconteçam no H4 finalizado
velas, espelhando o comportamento MetaTrader.

## Lógica de negociação
1. Assine o período definido por `CandleType` (o padrão é velas de 4 horas) e processe apenas velas concluídas.
2. Alimente um indicador `MovingAverageConvergenceDivergenceSignal` com o `FastPeriod`, `SlowPeriod` configurado e
`SignalPeriod`. O indicador fornece a linha MACD e a linha de sinal.
3. Calcule um filtro de tendência EMA com comprimento `TrendPeriod`. Sua inclinação determina se entradas longas ou curtas são permitidas.
4. Converta os limites baseados em pip (`MacdOpenLevelPips`, `MacdCloseLevelPips`, `TakeProfitPips`, `TrailingStopPips`) em absolutos
distâncias de preço usando o tamanho do pip do instrumento.
5. Quando não existe posição:
   - Abra uma posição **longa** se o MACD estiver abaixo de zero, o valor atual estiver acima do valor do sinal, o MACD anterior estiver abaixo
o sinal anterior, o EMA está aumentando e a magnitude MACD excede `MacdOpenLevelPips`.
   - Abra uma posição **curta** se o MACD estiver acima de zero, o valor atual estiver abaixo do valor do sinal, o MACD anterior estava acima
o sinal anterior, o EMA está caindo e a magnitude MACD excede `MacdOpenLevelPips`.
6. Enquanto mantém uma posição longa:
   - Feche a negociação quando MACD se tornar positivo, cruzar abaixo do sinal e sua magnitude exceder `MacdCloseLevelPips`.
   - Saia mais cedo se o preço atingir o take-profit configurado ou se o nível de trailing stop for violado.
7. Enquanto mantém uma posição curta:
   - Feche a negociação quando MACD ficar negativo, cruzar acima do sinal e sua magnitude exceder `MacdCloseLevelPips`.
   - Saia mais cedo se o preço atingir a meta de lucro ou o stop móvel.
8. O trailing stop é ativado somente depois que o preço ultrapassa o limite em `TrailingStopPips` e, em seguida, bloqueia o lucro em
seguindo os extremos subsequentes das velas.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `FastPeriod` | `int` | `47` | Comprimento EMA rápido usado dentro do cálculo MACD. |
| `SlowPeriod` | `int` | `166` | Comprimento EMA lento usado dentro do cálculo MACD. |
| `SignalPeriod` | `int` | `11` | EMA comprimento da linha de sinal MACD. |
| `TrendPeriod` | `int` | `8` | Comprimento do filtro de tendência EMA. |
| `MacdOpenLevelPips` | `decimal` | `1` | Magnitude mínima de MACD (em pips) necessária para abrir uma posição. |
| `MacdCloseLevelPips` | `decimal` | `3` | Magnitude mínima de MACD (em pips) necessária para fechar uma posição. |
| `TakeProfitPips` | `decimal` | `550` | Distância de lucro medida em pips. |
| `TrailingStopPips` | `decimal` | `19` | Distância do trailing-stop medida em pips. Um valor de `0` desativa o rastreamento. |
| `TradeVolume` | `decimal` | `1` | Volume líquido utilizado para entradas no mercado. |
| `CandleType` | `DataType` | Período de 4 horas | Série de velas processada pela estratégia. |
| `RequiredSecurityCode` | `string` | `EURUSD` | Código de segurança que deve corresponder ao instrumento selecionado, imitando a verificação MetaTrader. |

## Diferenças do especialista MetaTrader original
- MetaTrader gerencia pedidos individuais e números mágicos. StockSharp trabalha com posições líquidas, então a conversão fecha o
exposição atual e abre uma nova em vez de fazer malabarismos com vários tickets.
- O código original usava `AccountFreeMargin` para dimensionar posições dinamicamente. A porta StockSharp expõe um simples `TradeVolume`
parâmetro e documentos que os usuários devem configurar o dimensionamento da posição externamente.
- Os ajustes de stop-loss usam os extremos das velas de StockSharp em vez de modificar os pedidos existentes. As saídas ainda ocorrem no primeiro
vela que viola o trailing stop, produzindo um comportamento muito próximo da lógica MetaTrader.
- Todos os cálculos de indicadores dependem de classes de indicadores StockSharp vinculadas a `SubscribeCandles`, sem chamadas diretas para
Funções `iMACD` ou `iMA`.

## Notas de uso
- Atribua o instrumento desejado antes de iniciar a estratégia. Se o código do instrumento não corresponder a `RequiredSecurityCode` o
a estratégia para imediatamente para evitar a implantação acidental no mercado errado.
- `TradeVolume` é copiado para `Strategy.Volume` durante `OnStarted`, então os métodos auxiliares (`BuyMarket`, `SellMarket`) sempre usam o
tamanho configurado.
- Os trailing stops só se tornam ativos após o preço avançar além da distância configurada; até lá a estratégia dependerá da
MACD meta de cruzamento e lucro para saídas.
- Adicionar a estratégia a um gráfico atrai velas, ambos os indicadores e negociações executadas para que a lógica de cruzamento possa ser validada
visualmente.

## Indicadores
- `MovingAverageConvergenceDivergenceSignal` (MACD linha e linha de sinal).
- `ExponentialMovingAverage` (filtro de tendência).
