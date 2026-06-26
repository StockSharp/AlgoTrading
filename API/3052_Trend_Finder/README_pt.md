# Estratégia de Busca de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Trend Finder é uma estratégia de seguimento de tendência multiperíodo convertida do consultor especialista original **TREND FINDER.mq4**. A lógica agora usa a API de alto nível do StockSharp e mantém a ideia central de combinar médias móveis ponderadas linearmente com confirmações de momentum de períodos superiores e filtros MACD. A estratégia foca na detecção de rompimentos que seguem máximas ou mínimas sustentadas, visando entrar na direção do rompimento assim que o momentum e o alinhamento da tendência de longo prazo forem confirmados.

## Dados de mercado e indicadores
- **Período base (`CandleType`)** – velas primárias usadas para reconhecimento de padrões e execução de ordens. As médias móvias ponderadas linearmente são calculadas sobre o preço típico dessas velas.
- **Período de momentum (`MomentumCandleType`)** – velas de período superior usadas para avaliar desvios do momentum em relação ao valor neutro de 100. As três leituras de momentum mais recentes devem superar limites configuráveis antes de uma operação ser permitida.
- **Período MACD (`MacdCandleType`)** – velas de longo prazo processadas por um MACD com comprimentos de rápido, lento e sinal personalizáveis. Uma condição MACD altista (baixista) é necessária para configurações compradas (vendidas).

## Lógica de entrada
1. **Detecção de rompimento de tendência** – a estratégia escaneia até as últimas 100 velas históricas (excluindo as três mais recentes) para encontrar a máxima mais alta ou a mínima mais baixa. Uma configuração altista requer que a barra atual abra acima de um cluster anterior de máximas enquanto pelo menos uma das três máximas anteriores permaneça abaixo desse nível histórico. Uma configuração baixista espelha a lógica para mínimas.
2. **Alinhamento das médias móveis** – a LWMA rápida deve estar acima da LWMA lenta para comprados e abaixo para vendidos.
3. **Estrutura recente das velas** – para comprados, a mínima de duas barras atrás deve estar abaixo da máxima da barra anterior (`Low[2] < High[1]`), enquanto vendidos exigem que a última mínima esteja abaixo da máxima de duas barras atrás (`Low[1] < High[2]`). Isso preserva a verificação de estrutura de preço original.
4. **Confirmação de momentum** – pelo menos um dos três últimos desvios de momentum (calculados como |Momentum – 100|) no período superior deve superar os limites de compra/venda configurados.
5. **Confirmação MACD** – o último valor MACD no período de longo prazo deve estar acima de seu sinal para comprados e abaixo para vendidos.
6. **Filtro de posição** – novas ordens compradas são emitidas apenas quando a posição atual é não positiva, e novas ordens vendidas apenas quando é não negativa. O volume da ordem é igual a `Volume + |Position|` para suportar reversões rápidas de posição.

## Saída e gestão de risco
- **Stop-loss (`StopLoss`)** – distância fixa abaixo (acima) do preço de entrada para posições compradas (vendidas).
- **Take-profit (`TakeProfit`)** – alvo de lucro fixo; quando atingido, a posição é fechada imediatamente.
- **Trailing stop (`TrailingStop`)** – segue o preço mais alto atingido após entrar em uma posição comprada ou o preço mais baixo para vendidas. O stop é ajustado a cada vela concluída.
- **Ponto de equilíbrio (`BreakEvenTrigger`, `BreakEvenOffset`)** – assim que o preço se move a favor da operação pela distância de ativação, o stop de proteção é movido para o preço de entrada mais (menos) o offset para comprados (vendidos), garantindo que os lucros sejam bloqueados se o preço recuar.
- **Fechamento automático** – métodos auxiliares fecham todo o tamanho da posição e reiniciam todas as variáveis de rastreamento. Não há saídas parciais nesta implementação.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `CandleType` | Período base para reconhecimento de padrões e execução de ordens. | Velas de 15 minutos |
| `MomentumCandleType` | Período superior usado para calcular a confirmação de momentum. | Velas de 1 hora |
| `MacdCandleType` | Período para confirmação MACD (padrão ~30 dias). | Velas de 30 dias |
| `FastMaLength` | Comprimento da média móvel ponderada linearmente rápida. | 6 |
| `SlowMaLength` | Comprimento da média móvel ponderada linearmente lenta. | 85 |
| `MomentumPeriod` | Número de barras de período superior usadas para o ratio de momentum. | 14 |
| `MomentumThresholdBuy` | Mínimo |Momentum − 100| necessário para permitir entradas compradas. | 0.3 |
| `MomentumThresholdSell` | Mínimo |Momentum − 100| necessário para permitir entradas vendidas. | 0.3 |
| `MacdShortLength` | Comprimento do EMA rápido dentro do cálculo MACD. | 12 |
| `MacdLongLength` | Comprimento do EMA lento dentro do cálculo MACD. | 26 |
| `MacdSignalLength` | Comprimento do EMA de sinal para MACD. | 9 |
| `StopLoss` | Distância absoluta de stop-loss em unidades de preço do instrumento. | 0.0020 |
| `TakeProfit` | Distância absoluta de take-profit em unidades de preço do instrumento. | 0.0050 |
| `TrailingStop` | Distância do trailing stop que segue movimentos favoráveis. | 0.0040 |
| `BreakEvenTrigger` | Distância de lucro que ativa o stop de ponto de equilíbrio. | 0.0030 |
| `BreakEvenOffset` | Offset adicional aplicado assim que o ponto de equilíbrio está ativo. | 0.0010 |

> **Nota:** Defina a propriedade `Strategy.Volume` para o tamanho de ordem desejado antes de iniciar a estratégia. Os parâmetros acima são expressos em unidades de preço absolutas; ajuste-os de acordo com o tamanho do tick do instrumento negociado.

## Diretrizes de uso
1. Atribua a estratégia ao `Security` desejado e configure as propriedades `Portfolio` e `Volume`.
2. Certifique-se de que a fonte de dados selecionada possa entregar todos os três períodos de velas solicitados; caso contrário, os filtros de confirmação nunca estarão prontos.
3. Ajuste os parâmetros de risco para corresponder à volatilidade do instrumento. Como os padrões são expressos como distâncias de preço absolutas, podem precisar de reescalonamento para ações, futuros ou criptomoedas.
4. Opcionalmente, anexe a área de gráfico gerada para visualizar preço, trades e ambas as médias móveis.
5. Monitore logs para confirmações de ordens. A estratégia usa ordens de mercado (`BuyMarket`, `SellMarket`) para entradas e saídas.

## Diferenças do consultor especialista original
- Stops baseados em capital, lógica de take-profit baseada em saldo e notificações push/e-mail presentes no script MQL foram omitidos intencionalmente para manter a estratégia focada nas regras centrais de trading e para alinhar com a API de alto nível do StockSharp.
- O gerenciamento de volume está simplificado: a versão StockSharp abre no máximo uma posição líquida por vez e usa o `Volume` configurado para dimensionar trades.
- Parâmetros de gestão monetária expressos em moeda de conta não são replicados; em vez disso, controles de risco baseados em preço (`StopLoss`, `TakeProfit`, `TrailingStop`, ponto de equilíbrio) são fornecidos.

## Melhorias recomendadas
- Adicione controles de risco em nível de portfólio se vários símbolos forem negociados simultaneamente.
- Combine com filtros de sessão ou de volatilidade para desativar o trading durante períodos ilíquidos.
- Considere encaminhar execuções para análises externas (por exemplo, para rastreamento de capital) se tal funcionalidade for necessária.
