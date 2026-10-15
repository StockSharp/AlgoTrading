# Estratégia de Accelerator Trailing TP & SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Accelerator Trailing TP & SL porta o Consultor Especialista "Accelerator Trailing TP&SL" do MetaTrader para a API de alto nível do StockSharp. O sistema combina o Oscilador Acelerador de Bill Williams com confirmação de momentum multi-período e um filtro de tendência MACD mensal. As entradas são construídas com dimensionamento de posição geométrico enquanto as saídas combinam distâncias clássicas de stop/alvo, trailing adaptativo e lógica de break-even.

## Lógica de trading
- **Filtro de momentum** – um indicador de Momentum de 14 períodos calculado em um período superior deve desviar do nível neutro de 100 pelo menos pelo limiar configurado em qualquer uma das últimas três barras concluídas.
- **Oscilador Acelerador** – negociações longas requerem uma leitura positiva do acelerador, negociações curtas requerem uma leitura negativa no período de sinal.
- **Médias móvies** – uma média móvel ponderada linear rápida (LWMA) deve estar acima da LWMA lenta para comprados e abaixo para vendidos, aproximando o filtro de tendência rápido/lento original.
- **Tendência MACD mensal** – por padrão, o filtro observa velas mensais. Negociações longas exigem que a linha MACD esteja acima da linha de sinal (mesmo quando ambos os valores são negativos), enquanto negociações curtas requerem a condição oposta.
- **Entradas escalonadas** – a estratégia pode piramidizar até o número máximo configurado de posições por direção. Cada entrada adicional é multiplicada pelo expoente de lote, recriando o dimensionamento estilo martingale usado no programa MQL.

## Gestão de risco
- **Stop-loss / take-profit estático** – distâncias em pips refletem as configurações originais de Stop Loss e Take Profit.
- **Trailing stop** – quando habilitado, a estratégia segue o preço mais favorável pelo número de pips configurado.
- **Movimentação de break-even** – após uma negociação atingir a distância de gatilho, o stop é avançado pelo offset especificado, protegendo os lucros acumulados.
- **Saída MACD** – quando o filtro MACD se inverte contra a posição ativa, a estratégia pode fechar todas as posições imediatamente, correspondendo ao auxiliar de saída manual no código MQL.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `FastMaLength` / `SlowMaLength` | Períodos das LWMAs rápida e lenta no período de negociação. |
| `MomentumThreshold` | Desvio absoluto mínimo do momentum do valor neutro de 100 no período superior. |
| `StopLossPips` / `TakeProfitPips` | Distâncias de stop de proteção e alvo em pips. |
| `TrailingStopPips` | Distância usada pelo gerenciador de trailing stop opcional. |
| `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | Define quando e como o stop é movido para break-even. |
| `MaxTrades` | Número máximo de entradas escalonadas por direção. |
| `BaseVolume` | Volume da primeira ordem em uma sequência. |
| `LotExponent` | Multiplicador aplicado a cada entrada escalonada adicional. |
| `EnableTrailing` | Habilita ou desabilita o gerenciamento do trailing stop. |
| `UseBreakEven` | Habilita ou desabilita a movimentação do stop de break-even. |
| `CloseOnMacdFlip` | Fecha todas as negociações se o MACD do período superior se inverter. |
| `CandleType` | Série de velas principal para sinais (padrão: 15 minutos). |
| `MomentumCandleType` | Velas de período superior usadas pelo filtro de momentum (padrão: 1 hora). |
| `MacdCandleType` | Série de velas para o filtro de tendência MACD (padrão: velas mensais). |

## Notas
- A estratégia depende do `PriceStep` do instrumento para converter configurações de risco baseadas em pips para distâncias de preço. Certifique-se de que os metadados do ativo estejam preenchidos ao executar a estratégia.
- Como o StockSharp usa posições líquidas, entradas escalonadas adicionais são abertas enviando ordens de mercado repetidamente até que o máximo configurado seja atingido. As saídas fecham toda a posição líquida, correspondendo às rotinas "fechar tudo" no especialista original.
- O período MACD mensal pode ser ajustado através do parâmetro `MacdCandleType` para diferentes instrumentos ou backtests.
