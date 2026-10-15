# Estratégia DoubleTop do Macd Pattern Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Porta do consultor especialista MetaTrader 4 **MacdPatternTraderv04cb**. A estratégia verifica uma linha principal MACD configurável em busca de
padrões de topo duplo de baixa e fundo duplo de alta. Quando o segundo balanço não excede o primeiro enquanto o MACD
permanece além de um nível de gatilho positivo ou negativo, a estratégia abre uma posição de mercado na direção do
reversão prevista. As ordens de proteção reproduzem as distâncias originais de stop-loss fixas de 100 pips e de take-profit de 300 pips.

## Regras de negociação

1. Assine a série de velas selecionada (padrão: período de 30 minutos) e calcule a linha principal MACD com o
configurados períodos rápido, lento e de sinal (padrões: 5, 13 e 1).
2. Acompanhe os últimos três valores MACD concluídos. Uma configuração de baixa é armada quando o MACD permanece acima do `TriggerLevel`,
forma uma máxima local e depois declina. A configuração é validada quando o próximo máximo de MACD for menor que o armazenado anteriormente
alto enquanto o MACD ainda está acima do gatilho. Uma venda de mercado é enviada naquele momento.
3. Espelhe a mesma lógica abaixo de zero. Quando o MACD permanece abaixo de `-TriggerLevel`, forma um vale e o vale seguinte
for superior ao anterior, a estratégia abre uma compra no mercado.
4. Redefina os altos e baixos armazenados sempre que a linha MACD cruzar de volta para dentro do `[-TriggerLevel, TriggerLevel]`
alcance. Isso corresponde ao comportamento original EA que cancela a busca de padrão quando o momento perde força.
5. Os tamanhos das posições começam no `TradeVolume` configurado. Ao mudar de direção, a estratégia adiciona volume suficiente para
achatar a exposição oposta antes de estabelecer a nova negociação.
6. Chame `StartProtection` uma vez no início para que tanto o stop-loss de 100 pip quanto o take-profit de 300 pip sejam gerenciados pelo
plataforma mesmo após reinicializações.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `FastPeriod` | Comprimento EMA rápido usado por MACD. |
| `SlowPeriod` | Comprimento EMA lento usado por MACD. |
| `SignalPeriod` | Comprimento de suavização da linha de sinal para MACD. |
| `TriggerLevel` | Nível absoluto MACD necessário para armar a detecção de topo duplo/fundo duplo. |
| `StopLossPips` | Distância da parada de proteção em pips (padrão 100). |
| `TakeProfitPips` | Distância do take-profit em pips (padrão 300). |
| `TradeVolume` | Volume base de pedidos para novas posições. |
| `CandleType` | Série de velas usada para cálculos de indicadores. |

## Notas

- O stop-loss e o take-profit são convertidos de pips em etapas de instrumento antes de serem passados para
`StartProtection`, mantendo o comportamento idêntico ao especialista MQL4 original.
- Todos os comentários de indicadores e negociações dentro do código-fonte C# são escritos em inglês, conforme exigido pelo repositório
diretrizes.
