# Estratégia Crypto SR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Crypto SR porta o expert advisor "Crypto S&R" do MetaTrader 4 para a API de alto nível do StockSharp. A implementação preserva a lógica de confirmação em camadas do sistema original: um filtro de tendência baseado em médias móveis linearmente ponderadas (LWMA), uma checagem de momentum em timeframe superior, um filtro de tendência MACD de longo prazo e níveis de suporte/resistência derivados de fractais. As ordens são enviadas com execução a mercado e a posição é gerenciada por stop-loss/take-profit fixos, ajustes de break-even e trailing stop medido em pips.

## Lógica de negociação

1. **Análise do timeframe primário:** a estratégia assina a série de candles configurada e alimenta duas LWMAs com o preço típico do candle `(high + low + close) / 3`. A LWMA rápida deve permanecer acima (abaixo) da lenta para habilitar compras (vendas).
2. **Momentum em timeframe superior:** um indicador `Momentum` é avaliado em uma segunda série de candles. A distância absoluta das três últimas leituras de momentum em relação ao valor neutro (100) deve exceder os limiares de compra/venda.
3. **Filtro MACD de longo prazo:** a estratégia escuta outro fluxo de candles onde um MACD (12, 26, 9) é calculado. Posições compradas exigem que a linha MACD permaneça acima do sinal; posições vendidas precisam dela abaixo do sinal. O timeframe padrão de longo prazo é diário para aproximar a série mensal usada pelo EA; ele pode ser ajustado se candles mensais reais estiverem disponíveis.
4. **Suporte/resistência fractal:** candles concluídos são armazenados em um buffer rolante. Quando o padrão fractal clássico de Bill Williams (dois vizinhos de cada lado) aparece, a máxima/mínima correspondente vira o nível ativo de resistência ou suporte. Um buffer configurável em pips é aplicado ao redor do nível para emular as linhas horizontais desenhadas pelo expert original.
5. **Regras de entrada**:
   - *Compra*: sem posição comprada aberta, LWMA rápida acima da lenta, desvio de momentum >= limiar de compra, MACD altista, o candle atual testa o suporte com buffer e fecha acima do fechamento anterior.
   - *Venda*: condições espelhadas com o nível de resistência, limiar de venda do momentum e confirmação MACD baixista.
6. **Gestão de risco:** cada nova posição recebe stop-loss e take-profit iniciais em pips. A lógica de break-even pode mover o stop quando o movimento atinge a distância de disparo, enquanto um trailing stop opcional segue o preço usando máximas/mínimas dos candles. A exposição comprada/vendida é fechada se o filtro MACD virar contra a operação.

## Notas de implementação

- O filtro MACD mensal da versão MetaTrader é aproximado por padrão com uma série diária, porque o StockSharp não fornece candles de mês calendário diretamente. Usuários podem trocar para um agregador mensal personalizado se a fonte de dados permitir.
- Ordens são fechadas com requisições a mercado quando níveis de proteção são violados. Isso espelha as chamadas `OrderClose` em MQL e evita depender de ordens stop do lado da bolsa.
- Todas as vinculações de indicadores são feitas pela API de assinatura de alto nível, e chamadas diretas a `GetValue` não são necessárias.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `FastMaPeriod` | Comprimento da LWMA rápida no timeframe primário. | `6` |
| `SlowMaPeriod` | Comprimento da LWMA lenta no timeframe primário. | `85` |
| `MomentumPeriod` | Período do momentum no timeframe superior. | `14` |
| `MomentumBuyThreshold` | Desvio absoluto mínimo do momentum em relação a 100 para habilitar entradas compradas. | `0.3` |
| `MomentumSellThreshold` | Desvio absoluto mínimo do momentum em relação a 100 para habilitar entradas vendidas. | `0.3` |
| `MacdFastPeriod` | Comprimento da EMA rápida para o filtro MACD de longo prazo. | `12` |
| `MacdSlowPeriod` | Comprimento da EMA lenta para o filtro MACD de longo prazo. | `26` |
| `MacdSignalPeriod` | Comprimento da EMA de sinal para o filtro MACD de longo prazo. | `9` |
| `StopLossPips` | Distância do stop-loss rígido expressa em pips. | `20` |
| `TakeProfitPips` | Distância fixa do take-profit expressa em pips. | `50` |
| `TrailingStopPips` | Distância do trailing stop em pips (0 desativa o trailing). | `40` |
| `UseBreakEven` | Define se o stop é movido para break-even após um gatilho de lucro. | `true` |
| `BreakEvenTriggerPips` | Lucro em pips exigido antes de aplicar ajustes de break-even. | `30` |
| `BreakEvenOffsetPips` | Offset adicionado ao mover o stop para break-even. | `30` |
| `FractalWindowLength` | Número de candles concluídos retidos para confirmar máximas e mínimas fractais. | `7` |
| `FractalBufferPips` | Buffer adicional ao redor dos níveis fractais em pips. | `10` |
| `TradeVolume` | Volume enviado em cada ordem a mercado. | `1` |
| `CandleType` | Série primária de candles para LWMA e lógica fractal. | Timeframe `15m` |
| `HigherCandleType` | Timeframe superior para o filtro de momentum. | Timeframe `1h` |
| `LongTermCandleType` | Timeframe para o filtro de tendência MACD. | Timeframe `1d` |
