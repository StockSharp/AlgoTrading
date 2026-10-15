# Estratégia Alexav D1 Profit GBPUSD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Alexav D1 Profit GBPUSD é um sistema de breakout diário convertido do MetaTrader 4 consultor especialista *Alexav_d1_profit_gbpusd.mq4*. A estratégia opera em velas diárias de GBP/USD e avalia a sessão concluída uma vez por dia (terça a sexta). A confirmação do impulso é fornecida por RSI e MACD, enquanto as paradas ajustadas à volatilidade e as metas de lucro escalonadas são derivadas de ATR.

## Lógica de negociação
1. **Preparação de Indicadores**
   - Duas EMAs com o mesmo período são aplicadas aos preços máximos e mínimos diários para definir os níveis de referência de alta e baixa.
   - RSI com um lookback de 10 períodos mede o impulso. Leituras extremas de RSI bloqueiam temporariamente novas negociações nessa direção.
   - MACD (24/05/14) fornece um filtro de aceleração comparando os dois últimos valores do histograma.
   - ATR (28) fornece a unidade de volatilidade usada para stops e metas de lucro.
2. **Filtro de sessão**
   - Apenas uma avaliação é realizada para cada vela diária concluída de terça a sexta-feira. Segundas-feiras e fins de semana são ignorados.
3. **Configuração longa**
   - A vela diária anterior deve fechar acima de EMA de máximos calculados há duas sessões.
   - RSI da sessão anterior deve estar acima do nível superior (padrão 60), mas abaixo do limite superior (padrão 80).
   - MACD deve estar abaixo de zero há duas sessões ou mostrar uma aceleração positiva suficiente em comparação com o valor anterior.
   - Se a abertura anterior cair abaixo de EMA de máximos, a estratégia permite um novo lote de compras após a redefinição do bloco.
4. **Configuração breve**
   - Lógica de espelhamento da configuração longa, usando EMA de mínimos, RSI limites inferiores (39/25) e filtros MACD.

## Gerenciamento de ordens
Quando uma configuração é confirmada, a estratégia abre um lote de quatro ordens de mercado (cada uma usando a estratégia `Volume`):
- **Stops**: cada ordem compartilha o mesmo stop de proteção igual a `ATR * AtrStopMultiplier` (padrão 1,6) do preço de entrada.
- **Metas**: os objetivos de lucro são dimensionados em `AtrTargetMultiplier * (1 + i / 2)` para o índice de pedido `i` em `[0..3]`, replicando os deslocamentos 1,0, 1,5, 2,0 e 2,5 ATR do EA original.
- **Tratamento de conflitos**: Posições opostas são achatadas antes de abrir um novo lote. Acionar um lote longo limpa qualquer lote curto pendente (e vice-versa).

A estratégia monitora velas concluídas. Se a mínima diária tocar o stop, a ordem longa correspondente será fechada no mercado; se a máxima atingir a meta, a ordem também será fechada. As vendas são tratadas simetricamente usando a vela alta para stops e a mínima para alvos.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `CandleType` | Série de velas primárias, diariamente por padrão. | 1 dia |
| `MaPeriod` | Período do EMA aplicado a máximos/mínimos. | 6 |
| `RsiPeriod` | RSI período para filtro de impulso. | 10 |
| `AtrPeriod` | período ATR para dimensionamento de stop/alvo. | 28 |
| `AtrStopMultiplier` | ATR múltiplo para paradas. | 1.6 |
| `AtrTargetMultiplier` | Base ATR múltipla para destinos. | 1,0 |
| `RsiUpperLevel` | Limite de RSI confirmando o impulso de alta. | 60 |
| `RsiUpperLimit` | RSI limite que bloqueia novos longos. | 80 |
| `RsiLowerLevel` | Limite de RSI confirmando o impulso de baixa. | 39 |
| `RsiLowerLimit` | RSI piso que bloqueia novos shorts. | 25 |
| `FastMaPeriod` | Período rápido de EMA para MACD. | 5 |
| `SlowMaPeriod` | Período EMA lenta para MACD. | 24 |
| `SignalMaPeriod` | Período de sinal EMA para MACD. | 14 |
| `MacdDiffBuy` | Aceleração mínima MACD para longos. | 0,5 |
| `MacdDiffSell` | Aceleração mínima de MACD para shorts. | 0,15 |

Defina a estratégia `Volume` para o tamanho de lote desejado por pedido antes de iniciar a estratégia.

## Notas
- A conversão mantém a lógica de avaliação única por dia encontrada no consultor especialista original.
- Use dados históricos diários para GBP/USD ao fazer backtesting para reproduzir o comportamento pretendido.
- Paradas e alvos de proteção são simulados usando extremos de vela concluídos; picos intradiários dentro de uma vela diária não são visíveis para a estratégia.
