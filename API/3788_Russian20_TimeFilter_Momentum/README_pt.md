# Estratégia de impulso do filtro de tempo Russian20
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Russian20 Time Filter Momentum** é uma conversão do MetaTrader 4 consultor especialista `Russian20-hp1.mq4`, originalmente distribuído pela Gordago Software Corp. O algoritmo combina uma média móvel simples de 20 períodos (SMA) com um indicador Momentum de 5 períodos avaliado em velas de 30 minutos. As posições só são abertas quando a dinâmica do preço e a direção da tendência se alinham, opcionalmente restritas a uma janela de negociação intradiária definida pelo usuário.

## Lógica de negociação
- **Frequência de dados:** usa o tipo de vela configurável (padrão: velas de 30 minutos, correspondendo a `PERIOD_M30` do script MT4). Todos os sinais são avaliados apenas em velas totalmente fechadas para permanecerem fiéis à execução de fechamento da barra do especialista original.
- **Indicadores:**
  - Média Móvel Simples com comprimento ajustável (padrão 20).
  - Indicador de impulso com lookback configurável (padrão 5) e um nível neutro definido como 100, assim como em MetaTrader.
- **Entrada longa:** Acionada quando as seguintes condições se alinham na última barra fechada:
  1. O preço de fechamento está acima de SMA.
  2. O impulso é impresso acima do limite neutro (padrão 100).
  3. O preço de fechamento atual é superior ao fechamento da vela anterior.
- **Entrada curta:** Acionada quando:
  1. O preço de fechamento está abaixo de SMA.
  2. O momentum está abaixo do limite neutro.
  3. O preço de fechamento atual é inferior ao fechamento anterior.
- **Regras de saída:**
  - As posições longas são fechadas quando o Momentum cai para ou abaixo do limite ou quando a meta de lucro (se habilitada) é atingida.
  - As posições curtas são fechadas quando o Momentum sobe para ou acima do limite ou quando a meta de lucro é alcançada.

## Filtro de sessão
O script MetaTrader ofereceu uma janela de negociação opcional (padrão 14h00–16h00). A porta StockSharp expõe o mesmo comportamento por meio dos parâmetros `UseTimeFilter`, `StartHour` e `EndHour`. Quando o filtro está ativo, a estratégia pula entradas e saídas fora do horário selecionado, espelhando a lógica de retorno antecipado do especialista original.

## Gestão de risco
A versão MQL4 anexou um lucro fixo de 20 pip a cada pedido. A conversão mantém esse recurso e expressa a distância em “pips”, ajustando automaticamente o preço do pip fracionário (3/5 decimais) por meio do `PriceStep` do instrumento. Definir `TakeProfitPips` como zero desativa totalmente a meta de lucro.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | Velas de 30 minutos | Tipo de dados usado para cálculos de preços/indicadores. |
| `MovingAverageLength` | 20 | Lookback para o filtro de tendência SMA. |
| `MomentumPeriod` | 5 | Lookback para o indicador Momentum. |
| `MomentumThreshold` | 100 | Nível de Momentum Neutro usado para entradas e saídas. |
| `TakeProfitPips` | 20 | Distância alvo de lucro em pips. Zero desativa o alvo. |
| `UseTimeFilter` | falso | Habilita o filtro de sessão de negociação intradiária. |
| `StartHour` | 14 | Hora de início inclusiva da janela de negociação (0–23). |
| `EndHour` | 16 | Hora final inclusiva da janela de negociação (0–23). |

Todos os parâmetros são definidos por meio de `StrategyParam<T>`, mantendo-os visíveis na UI e prontos para otimização.

## Notas de implementação
- Usa o `SubscribeCandles().Bind(...)` API de alto nível para que os valores dos indicadores sejam transmitidos diretamente para a rotina de processamento sem gerenciamento manual de séries.
- Armazena apenas o último preço de fechamento para comparar velas consecutivas, evitando consultas históricas pesadas e cumprindo as diretrizes de desempenho do repositório.
- Recalcula automaticamente o multiplicador pip de `Security.PriceStep`, garantindo distâncias corretas de lucro entre símbolos Forex com preços de 4/5 dígitos.
- Adiciona ganchos opcionais de renderização de gráficos (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) para análise visual conveniente quando o ambiente host oferece suporte.

## Dicas de uso
- Alinhe o tipo de vela com o prazo que você pretende negociar; para pares Forex, a configuração original de 30 minutos é um ponto de partida razoável.
- Quando `UseTimeFilter` estiver ativado, certifique-se de que `StartHour` seja menor ou igual a `EndHour`. Definir a hora de início depois da hora de término desativa efetivamente a negociação porque a lógica MT4 simplesmente ignorou o processamento fora do intervalo especificado.
- Como o especialista nunca usou um stop-loss, considere combinar a estratégia com controles de risco adicionais (manuais ou por meio de StockSharp recursos de proteção) ao negociar capital real.
