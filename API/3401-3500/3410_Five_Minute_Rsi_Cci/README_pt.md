# Estratégia FiveMinuteRsiCci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

`FiveMinuteRsiCciStrategy` é uma porta StockSharp do consultor especialista MetaTrader 4 **5Mins Rsi Cci EA.mq4**. O script original negocia velas de cinco minutos combinando um cruzamento de limite RSI com um filtro de média móvel suavizado/EMA e a polaridade de dois indicadores CCI. A versão C# mantém a mesma lógica de decisão ao usar o API de alto nível do API para assinaturas de dados, vinculação de indicadores e gerenciamento de risco.

## Lógica de negociação

1. Assine o tipo de vela configurado (período de cinco minutos por padrão) e atualize cinco indicadores em tempo real: RSI, um MA suavizado do preço de abertura, um EMA do preço de abertura, além de CCIs rápidos e lentos calculados a partir de preços típicos.
2. Cada vela finalizada é avaliada apenas quando nenhuma posição está aberta e o spread de compra/venda atual está abaixo de `MaxSpreadPoints` (convertido em unidades de preço).
3. Um sinal longo requer:
   - o MA suavizado acima de EMA,
   - o RSI cruzando para cima através de `BullishRsiLevel` entre a vela anterior e a atual,
   - ambos os valores CCI acima de zero.
4. Um sinal curto requer as condições inversas (MA suavizada abaixo de EMA, RSI cruzando para baixo através de `BearishRsiLevel`, ambos os CCIs abaixo de zero).
5. O volume do pedido reproduz o dimensionamento da posição dinâmica do EA: `LotCoefficient × sqrt(Equity / EquityDivisor)` arredondado para a etapa de volume do instrumento e limitado por `VolumeMin`/`VolumeMax`.
6. A lógica de proteção é tratada por `StartProtection`, que anexa distâncias de stop-loss, take-profit e trailing-stop convertidas de MetaTrader pontos em compensações de preço absoluto.

## Parâmetros

| Parâmetro | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Prazo usado para atualizações de indicadores e avaliação de sinais. |
| `RsiPeriod` | `14` | Número de velas usadas no cálculo RSI. |
| `FastSmmaPeriod` | `2` | Período da média móvel suavizada rápida aplicada aos preços de abertura. |
| `SlowEmaPeriod` | `6` | Período de lentidão EMA aplicado aos preços de abertura. |
| `FastCciPeriod` | `34` | Período do rápido CCI calculado a partir do preço típico `(H+L+C)/3`. |
| `SlowCciPeriod` | `175` | Período de lentidão CCI calculado a partir do preço típico. |
| `BullishRsiLevel` | `55` | RSI limite que deve ser ultrapassado para cima para armar uma entrada longa. |
| `BearishRsiLevel` | `45` | RSI limite que deve ser ultrapassado para baixo para armar uma entrada curta. |
| `StopLossPoints` | `60` | Distância stop-loss em MetaTrader pontos (convertido em preço absoluto). Defina como `0` para desativar. |
| `TakeProfitPoints` | `0` | Distância de lucro em MetaTrader pontos. Zero mantém o comportamento original EA (sem TP). |
| `TrailingStopPoints` | `20` | Distância do trailing-stop em MetaTrader pontos. Zero desativa o rastreamento. |
| `LotCoefficient` | `0.01` | Coeficiente base usado na fórmula de dimensionamento de posição dinâmica. |
| `EquityDivisor` | `10` | Divisor dentro da raiz quadrada para dimensionamento baseado em patrimônio (`sqrt(Equity / EquityDivisor)`). |
| `MaxSpreadPoints` | `18` | Spread máximo permitido (em MetaTrader pontos). Os pedidos são ignorados até que o spread diminua. |

## Notas

- O filtro de propagação depende de dados de nível 1; se as melhores cotações de compra/venda não estiverem disponíveis, a estratégia espera antes de abrir novas posições.
- A conversão ponto-preço é dimensionada automaticamente em `PriceStep` e a precisão do instrumento (5/3 instrumentos decimais multiplicam o passo por 10) para espelhar o valor `Point` de MetaTrader.
- Stops e trailing são gerenciados por meio do mecanismo de proteção integrado do StockSharp com saídas de mercado, correspondendo ao uso de ordens de mercado do EA para atualizações de trailing stop.
