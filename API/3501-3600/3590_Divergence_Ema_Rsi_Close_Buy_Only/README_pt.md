# Divergência + EMA + RSI Fechar somente compra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia transfere o consultor especialista MetaTrader "Divergence + ema + rsi close buy only" para o API de alto nível de StockSharp. Ele atua em **velas de 5 minutos** enquanto consulta dados **horários** e **diários** para confirmar o alinhamento de tendências e condições de sobrevenda. Os pedidos são apenas longos. As entradas exigem uma divergência de alta do histograma MACD que é confirmada por um cruzamento estocástico horário dentro de uma faixa estreita de sobrevenda e por uma estrutura diária crescente EMA. As saídas dependem de um overshoot fixo de RSI combinado com proteção opcional de stop-loss e take-profit gerenciada pela estrutura.

## Lógica de negociação

1. **Filtro de tendência de período de tempo mais alto**
   - O EMA(9) diário deve estar acima de EMA(20) para garantir uma tendência de alta prevalecente.
   - O último fechamento de 5 minutos deve permanecer abaixo do EMA(9) diário para que entradas longas sejam tentadas a partir de retrocessos.

2. **Confirmação estocástica por hora**
   - O valor %K estocástico horário concluído mais recente deve estar entre `StochasticLowerBound` (padrão 0) e `StochasticUpperBound` (padrão 40).
   - %K deve ter ultrapassado %D na última barra horária (%K atual > %D enquanto o %K anterior ≤ %D anterior).

3. **MACD gatilho de divergência (5 minutos)**
   - O histograma MACD (linha MACD menos linha de sinal) deve melhorar em pelo menos `MacdThreshold` enquanto o fechamento de 5 minutos define um mínimo mais baixo em comparação com a vela anterior. Isso se aproxima da divergência de alta usada pelo EA original.

4. **Execução de entrada**
   - Quando todos os filtros estão alinhados e nenhuma posição comprada está aberta, a estratégia envia uma compra de mercado. Se existir uma posição curta inesperada, o volume solicitado é aumentado para neutralizá-la antes de lançar uma posição longa.

5. **Regras de saída**
   - Uma saída protetora RSI fecha o comprimento quando o RSI de 5 minutos cruza acima de `RsiExitLevel` (padrão 77).
   - `StartProtection` ativa os níveis de stop-loss e take-profit convertidos de pips em distâncias de preço sempre que os parâmetros correspondentes forem positivos.

6. **Gerenciamento de pedidos**
   - Todas as ordens ativas são canceladas antes do envio de uma nova ordem de compra de mercado para evitar preenchimentos duplicados.
   - O volume padrão é o parâmetro `TradeVolume` e pode ser ajustado para otimização.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-------------|---------|
| `CandleType` | Prazo principal para MACD, RSI e execução. | Velas de 5 minutos |
| `HourTimeFrame` | Período horário usado pelo filtro estocástico. | 1 hora |
| `DayTimeFrame` | Prazo diário para confirmação da tendência EMA. | 1 dia |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Estrutura MACD no período primário. | 6/13/5 |
| `MacdThreshold` | Aumento mínimo de MACD no histograma para aceitar uma divergência. | 0,0003 |
| `DailyFastPeriod` / `DailySlowPeriod` | Períodos diários EMA. | 20/09 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | Configuração estocástica horária. | 30/5/9 |
| `StochasticUpperBound` / `StochasticLowerBound` | Faixa de %K por hora aceita. | 40/0 |
| `RsiPeriod` | Duração de RSI no período principal. | 7 |
| `RsiExitLevel` | Valor RSI que força saídas longas. | 77 |
| `TradeVolume` | Tamanho base do pedido para compras. | 0,01 |
| `StopLossPips` | Distância de stop-loss em pips (0 desativa). | 100 |
| `TakeProfitPips` | Distância de lucro em pips (0 desabilita). | 200 |

## Notas

- A estratégia assina três fluxos de dados: o período primário configurado, uma série horária e uma série diária. Cada fluxo aciona seu próprio conjunto de indicadores via `Bind`/`BindEx` para manter a implementação concisa e orientada a eventos.
- Os valores dos indicadores são processados apenas em velas finalizadas para espelhar os parâmetros de deslocamento do EA original.
- A detecção de divergência MACD usa o valor de fechamento e histograma da barra anterior como uma aproximação simples, porém robusta, da lógica gerada pelo construtor a partir do arquivo de origem MQL.
- Stop-loss e take-profit são gerenciados por `StartProtection` para permanecerem sincronizados com os preenchimentos do corretor e oferecer suporte a backtesting ou negociação ao vivo sem replicação manual de pedidos.
