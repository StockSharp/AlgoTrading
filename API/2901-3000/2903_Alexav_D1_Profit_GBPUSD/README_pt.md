# Estratégia Alexav D1 Profit GBPUSD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de rompimento diário para GBPUSD que combina uma EMA calculada em máximas, filtros RSI, confirmação de momentum MACD e gestão de risco baseada em ATR. O script reproduz o comportamento de quatro tomadas de lucro e ponto de equilíbrio da versão MetaTrader original.

## Dados Principais

- **Mercado**: GBP/USD spot ou CFD
- **Período**: Candles diários (configurável)
- **Direção**: Comprado e vendido
- **Estilo de posição**: Escalonamento multi-alvo com stop-loss compartilhado
- **Instrumentos Usados**: EMA (High), RSI, linha principal MACD, ATR

## Configuração de Indicadores

1. **EMA em preços Máximos** – comprimento padrão 6, aproxima o nível dinâmico de rompimento.
2. **RSI** – comprimento padrão 10, define os corredores de sobrecompra/sobrevenda usados como filtros de momentum.
3. **Linha principal MACD** – rápido 5, lento 21, sinal 14. Apenas a linha principal é usada para medir a inclinação do momentum.
4. **ATR** – comprimento 28, fornece stops e alvos dependentes de volatilidade.

## Lógica de Entrada

### Entradas Compradas

1. A barra diária anterior abre abaixo da EMA (High) e fecha acima dela (confirmação de cruzamento ascendente).
2. O RSI permanece entre **60** e **80** – previne trades durante momentum fraco e evita rallys sobreextendidos.
3. A linha principal do MACD satisfaz uma de duas verificações de momentum:
   - O valor dois barras atrás é negativo (indicando que a tendência recentemente se tornou positiva), **ou**
   - A redução relativa no MACD absoluto entre as últimas duas barras supera o limiar configurável **MacdDiffBuy** (padrão 0.5).

Se todas as condições forem atendidas, quatro ordens iguais de compra de mercado são colocadas (0.1 lotes cada uma por padrão). Qualquer exposição vendida existente é zerada antes de enviar o novo lote.

### Entradas Vendidas

1. A barra abre acima da EMA (High) e fecha abaixo dela.
2. O RSI está entre **25** e **39** – espelha os limiares do lado comprado.
3. O MACD dois barras atrás é positivo **ou** a mudança relativa no MACD absoluto entre as últimas duas barras está acima de **MacdDiffSell** (padrão 0.15).

Na confirmação, a estratégia zera os comprados existentes, depois envia quatro vendas iguais de mercado.

## Gestão de Trades

- **Stop Inicial**: Stop ATR compartilhado calculado a partir do fechamento de entrada. Comprados usam `entry - ATR * StopLossMultiplier` (padrão 1.6). Vendidos usam `entry + ATR * StopLossMultiplier`.
- **Alvos de Lucro**: Quatro níveis incrementais baseados em ATR por direção: múltiplos `1.0`, `1.5`, `2.0` e `2.5` escalados pelo parâmetro `TakeProfitMultiplier` (padrão 1). Cada nível fecha um quarto da posição original através de uma ordem de mercado quando o preço passa pelo nível.
- **Comportamento de Ponto de Equilíbrio**: Após cada saída parcial, o stop protetor para a posição restante é movido para o preço alvo mais recente. Isso imita o EA original que modifica os stop-losses para o preço de take-profit executado sempre que um trade TP ocorre.
- **Tratamento de Stop**: Se o preço tocar o nível protetor intrabar (usando máxima/mínima do candle), a posição restante é fechada imediatamente a mercado.

## Notas de Controle de Risco

- A estratégia não piramida além do lote de quatro entradas. Um novo sinal é ignorado enquanto a exposição permanece na mesma direção.
- O ATR deve ser positivo; os sinais são ignorados se o indicador de volatilidade ainda não se formou.
- Mudanças de parâmetros em tempo de execução afetam apenas ordens futuras; o volume por ordem é capturado na entrada para o escalonamento correto nas saídas.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-------------|---------|
| `OrderVolume` | Volume por ordem individual de mercado no lote | `0.1` |
| `EmaPeriod` | Comprimento de EMA aplicado a máximas de candle | `6` |
| `RsiPeriod` | Período médio do RSI | `10` |
| `AtrPeriod` | Período médio do ATR | `28` |
| `StopLossMultiplier` | Múltiplo ATR para o stop protetor | `1.6` |
| `TakeProfitMultiplier` | Múltiplo ATR base para alvos de lucro | `1.0` |
| `MacdFastPeriod` | Comprimento de EMA rápida do MACD | `5` |
| `MacdSlowPeriod` | Comprimento de EMA lenta do MACD | `21` |
| `MacdSignalPeriod` | Comprimento de EMA de sinal do MACD | `14` |
| `MacdDiffBuyThreshold` | Melhoria mínima de inclinação MACD para trades comprados | `0.5` |
| `MacdDiffSellThreshold` | Melhoria mínima de inclinação MACD para trades vendidos | `0.15` |
| `RsiUpperLimit` | RSI máximo permitido antes de uma entrada comprada | `80` |
| `RsiUpperLevel` | RSI mínimo necessário para uma entrada comprada | `60` |
| `RsiLowerLevel` | RSI máximo permitido para uma entrada vendida | `39` |
| `RsiLowerLimit` | RSI mínimo necessário antes de vendidos | `25` |
| `CandleType` | Período usado para a assinatura de candles | `1 Day` |

## Dicas de Implantação

- Otimizar os limiares de RSI e MACD juntos; afrouxar os corredores RSI sem ajustar os filtros de aceleração MACD pode criar sinais falsos.
- Como as saídas parciais dependem dos extremos do candle, dados precisos de valores máximos/mínimos são importantes para backtests realistas.
- Sempre operar com capital suficiente para lidar com quatro ordens simultâneas por sinal.
