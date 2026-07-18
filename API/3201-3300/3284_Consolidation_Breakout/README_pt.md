# Estratégia de rompimento de consolidação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o comportamento central do expert advisor original **Consolidation Breakout** para MetaTrader. Ela procura consolidações estreitas confirmadas por filtros de momentum e MACD, depois abre uma posição na direção do rompimento. O risco é gerenciado por distâncias fixas de take-profit e stop-loss medidas em passos de preço (pips).

## Como funciona

1. O período primário é definido pelo parâmetro `CandleType`. Todas as verificações de tendência e consolidação são avaliadas nesses candles.
2. Duas médias móveis ponderadas lineares (LWMAs), calculadas no preço típico, fornecem o filtro direcional. Configurações compradas exigem que a LWMA rápida permaneça acima da LWMA lenta, enquanto configurações vendidas precisam do alinhamento oposto.
3. Uma consolidação é detectada quando a mínima do candle de duas barras atrás permanece abaixo da máxima do candle anterior (caso comprado) ou quando a mínima anterior fica abaixo da máxima de duas barras atrás (caso vendido). Isso espelha a lógica de barras sobrepostas da versão MQL.
4. O momentum deve confirmar o movimento. O valor absoluto de momentum (relativo a zero) precisa exceder o respectivo limite de compra ou venda. Isso aproxima o filtro de momentum do expert original em torno do nível 100.
5. Um MACD separado calculado no período `MacdCandleType` deve concordar com a direção da operação. A estratégia verifica se a linha MACD lidera a linha de sinal nos lados positivo e negativo do eixo, reproduzindo a confirmação multitemporal do código-fonte.
6. Quando todos os filtros se alinham e a conta está zerada ou posicionada na direção oposta, a estratégia envia uma ordem a mercado dimensionada por `TradeVolume`. Níveis de proteção são imediatamente recalculados em passos de preço para que extremos intrabar possam acionar saídas.
7. Cada candle finalizado também monitora posições ativas. Se o intervalo do candle tocar o nível de stop-loss ou take-profit, a estratégia fecha a posição a mercado e redefine os alvos de proteção.

## Indicadores

- Média móvel ponderada linear (rápida e lenta, preço típico)
- Momentum
- MACD (com períodos 12/26/9 em um período mais alto)

## Parâmetros

- `CandleType` - período primário usado para detecção de rompimento.
- `MacdCandleType` - período usado para o filtro MACD de confirmação.
- `FastMaPeriod` - comprimento da LWMA rápida.
- `SlowMaPeriod` - comprimento da LWMA lenta.
- `MomentumLength` - retrospectiva do filtro de momentum.
- `MomentumBuyThreshold` - momentum positivo mínimo exigido para operações compradas.
- `MomentumSellThreshold` - momentum negativo mínimo exigido para operações vendidas (expresso como valor absoluto).
- `StopLossPips` - distância do stop de proteção em passos de preço.
- `TakeProfitPips` - distância do alvo de lucro em passos de preço.
- `TradeVolume` - volume enviado com cada ordem a mercado.

Os padrões espelham o expert advisor publicado: períodos LWMA de 6 e 85, momentum length 14, limites de compra/venda de 0.3, stop-loss de 20 pips e take-profit de 50 pips. Ajuste as distâncias baseadas em pips ao negociar instrumentos com passos de preço diferentes.

## Observações

- Trailing stops, movimentos de break-even e módulos de gestão de dinheiro do script MQL são omitidos intencionalmente para manter a versão StockSharp focada na lógica central de rompimento.
- Sempre garanta que os períodos selecionados sejam suportados pelo seu feed de dados. Se o período mais alto produzir dados esparsos, considere mudar para um `MacdCandleType` menor para manter o filtro MACD responsivo.
