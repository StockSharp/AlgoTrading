# Estratégia de Candle Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Candle Trailing Stop** é uma portagem do StockSharp do expert advisor MetaTrader com o mesmo nome. O robô original combinava filtros de tendência multitemporal, confirmação de momentum e um motor de trailing stop agressivo que seguia as mínimas e máximas das velas recentes. A versão C# mantém o mesmo fluxo de trabalho, mas se apoia em componentes de alto nível do StockSharp e expõe todas as configurações críticas como parâmetros de estratégia.

## Lógica principal

1. **Assinaturas de dados**
   - O período de negociação impulsiona as entradas e as atualizações do trailing stop.
   - Um período superior fornece confirmação usando médias móveis linearmente ponderadas (LWMA) e um indicador de momentum.
   - Uma terceira assinatura calcula uma linha MACD em um período lento (mensal por padrão) para filtrar as operações.
2. **Alinhamento de tendência**
   - As operações são permitidas apenas quando as sequências de LWMA rápida, média e lenta estão alinhadas em ambos os períodos (sequência de alta para comprados, de baixa para vendidos).
3. **Portão de momentum**
   - O indicador de momentum deve estar próximo do valor neutro de 100 em pelo menos uma das últimas três barras do período superior.
4. **Confirmação de MACD**
   - Comprados exigem que a linha MACD esteja acima da linha de sinal; vendidos exigem a relação inversa.
5. **Gatilho de entrada**
   - Uma ruptura através da LWMA rápida no período atual (vela fechando acima/abaixo da média após tocá-la na barra anterior) inicia novas operações respeitando um limite de posição configurável.
6. **Gestão de risco e saída**
   - As distâncias iniciais de stop-loss e take-profit são definidas em pips e automaticamente convertidas em passos de preço.
   - Os stops podem migrar para o ponto de equilíbrio, seguir o extremo das velas recentes, ou recuar para um trailing clássico de distância fixa.
   - Funções opcionais baseadas em capital espelham o EA original: take profit monetário, take profit percentual, trailing de capital e proteção contra drawdown.

## Parâmetros

| Grupo | Nome | Descrição | Padrão |
|--------------|-------------------------|---------------------------------------------------------------------------------------------|---------|
| Negociação | `Volume` | Tamanho da ordem em lotes/contratos. | `1` |
| | `MaxTrades` | Exposição agregada máxima expressa como `Volume * MaxTrades`. | `10` |
| Indicadores | `FastCurrentLength` | LWMA rápida no período de negociação. | `9` |
| | `MiddleCurrentLength` | LWMA média no período de negociação. | `20` |
| | `SlowCurrentLength` | LWMA lenta no período de negociação. | `52` |
| | `FastHigherLength` | LWMA rápida no período superior. | `9` |
| | `MiddleHigherLength` | LWMA média no período superior. | `20` |
| | `SlowHigherLength` | LWMA lenta no período superior. | `52` |
| | `MomentumPeriod` | Período de momentum no período superior. | `14` |
| | `MomentumBuyThreshold` | Desvio máximo de 100 permitido para operações compradas. | `0.3` |
| | `MomentumSellThreshold` | Desvio máximo de 100 permitido para operações vendidas. | `0.3` |
| | `MacdFastLength` | Comprimento da EMA rápida para confirmação MACD. | `12` |
| | `MacdSlowLength` | Comprimento da EMA lenta para confirmação MACD. | `26` |
| | `MacdSignalLength` | Comprimento da EMA de sinal para confirmação MACD. | `9` |
| Risco | `StopLossPips` | Distância do stop-loss em pips. | `20` |
| | `TakeProfitPips` | Distância do take-profit em pips. | `50` |
| | `UseMoveToBreakEven` | Ativa a lógica de ponto de equilíbrio. | `true` |
| | `BreakEvenTriggerPips` | Lucro em pips necessário antes de mover o stop. | `30` |
| | `BreakEvenOffsetPips` | Offset adicionado ao deslocar o stop para o ponto de equilíbrio. | `30` |
| | `UseCandleTrail` | Escolher entre trailing baseado em velas (`true`) ou trailing clássico (`false`). | `true` |
| | `CandleTrailLength` | Número de velas fechadas usadas para calcular os extremos de trailing. | `3` |
| | `PadAmountPips` | Buffer extra adicionado abaixo/acima do extremo de trailing. | `10` |
| | `TrailTriggerPips` | Lucro necessário antes de o trailing clássico ativar. | `40` |
| | `TrailAmountPips` | Distância mantida pelo trailing clássico. | `40` |
| Regras de capital | `UseMoneyTakeProfit` | Fechar todas as posições quando o lucro flutuante exceder o alvo monetário. | `false` |
| | `MoneyTakeProfit` | Alvo de lucro monetário. | `40` |
| | `UsePercentTakeProfit` | Fechar todas as posições quando o lucro flutuante exceder o alvo percentual. | `false` |
| | `PercentTakeProfit` | Percentual do capital inicial usado como alvo de lucro. | `10` |
| | `EnableMoneyTrailing` | Ativa trailing do lucro flutuante após um limite. | `true` |
| | `MoneyTrailTarget` | Nível de lucro que ativa a lógica de trailing monetário. | `40` |
| | `MoneyTrailStop` | Retração máxima permitida após o alvo ser atingido. | `10` |
| | `UseEquityStop` | Ativa proteção contra drawdown de capital. | `true` |
| | `EquityRiskPercent` | Drawdown máximo do pico de capital antes de forçar posição plana. | `1` |
| Dados | `CurrentCandleType` | Período de negociação. | `5m` |
| | `HigherCandleType` | Período superior usado para filtros. | `30m` |
| | `MacdCandleType` | Período para confirmação MACD (mensal por padrão). | `30d` |

## Notas e suposições

- Pips são convertidos em passos de preço usando o tamanho de tick do instrumento. Em símbolos onde um pip difere de um tick pode ser necessário ajustar as distâncias de pip padrão.
- Funções monetárias dependem do lucro não realizado aproximado como `(fechamento - preçoMédio) * posição`. Ajustes de swap e comissão não são simulados.
- A estratégia usa ordens a mercado para entradas e saídas. As ordens iniciais de take-profit são registradas assim que uma operação é aberta, enquanto o gerenciamento de stop-loss é tratado internamente e fecha através de ordens a mercado quando o nível calculado é cruzado.
- Todos os comentários no código são escritos em inglês conforme as diretrizes do projeto.
