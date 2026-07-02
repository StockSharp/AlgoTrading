# Estratégia Eli
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia Elli transporta o consultor especialista MetaTrader 4 "Elli" para o StockSharp API de alto nível. O robô original combinou a estrutura Ichimoku Kinko Hyo no período H1 com um filtro de período inferior ADX e parâmetros de risco rígidos. A conversão mantém a mesma lógica direcional, substitui o gerenciamento manual de pedidos por `StartProtection` e expõe cada botão de ajuste como um `StrategyParam<T>` otimizável para que o comportamento possa ser adaptado a diferentes mercados.

## Lógica de negociação
1. **Ichimoku estrutura de tendência**
   - A estratégia segue o período definido por `CandleType` (H1 por padrão) e calcula os períodos Tenkan-sen, Kijun-sen e Senkou usando os períodos originais (19, 60, 120).
   - Uma configuração de alta requer Tenkan > Kijun > Senkou Span A > Senkou Span B com a vela fechada acima de Kijun. As configurações de baixa refletem essa condição.
   - A distância absoluta entre Tenkan e Kijun deve exceder `TenkanKijunGapPips` pips para evitar nuvens planas ou extensas.
2. **Confirmação de movimento direcional**
   - Uma segunda assinatura de vela executa o Índice Direcional Médio no período especificado por `AdxCandleType` (M1 por padrão).
   - Sinais longos são permitidos somente quando o valor +DI anterior estiver abaixo de `ConvertLow` e o +DI atual estiver acima de `ConvertHigh`. Os shorts requerem o mesmo relacionamento para o componente −DI, replicando o filtro de aceleração presente no código MT4.
3. **Execução de entrada**
   - Quando todos os filtros estão alinhados, a estratégia emite uma ordem de mercado com volume `OrderVolume + |Position|`. Isso fecha automaticamente qualquer exposição oposta antes de aderir à tendência.
   - Apenas uma exposição direcional é mantida por vez, seguindo a guarda `OrdersTotal() < 1` original.
4. **Gerenciamento de riscos**
   - `StartProtection` anexa ordens simétricas de stop loss e takeprofit convertidas de distâncias de pip usando o tamanho do pip do instrumento.
   - Caso contrário, a posição é gerenciada passivamente, permitindo que as ordens de proteção lidem com as saídas da mesma forma que o consultor especialista MT4.

## Indicadores e assinaturas de dados
- Velas primárias: `CandleType` (velas padrão de 1 hora) para processamento de Ichimoku.
- ADX velas: `AdxCandleType` (velas padrão de 1 minuto) para verificações de aceleração DI.
- Indicadores: `Ichimoku` (Tenkan, Kijun, Senkou Span B) e `AverageDirectionalIndex` (fornecendo +DI/−DI).
- Ambas as assinaturas suportam renderização de gráfico por meio de `DrawCandles`, `DrawIndicator` e `DrawOwnTrades` se uma área de gráfico estiver disponível.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `OrderVolume` | `1` | Volume base de ordens de mercado. |
| `TakeProfitPips` | `60` | Distância de lucro expressa em pips. |
| `StopLossPips` | `30` | Distância de stop-loss expressa em pips. |
| `TenkanPeriod` | `19` | Período Tenkan-sen para o indicador Ichimoku. |
| `KijunPeriod` | `60` | Período Kijun-sen para o indicador Ichimoku. |
| `SenkouSpanBPeriod` | `120` | Período Senkou Span B para a nuvem Ichimoku. |
| `TenkanKijunGapPips` | `20` | Distância mínima Tenkan/Kijun (em pips) exigida antes da negociação. |
| `ConvertHigh` | `13` | Limite DI que o valor atual deve exceder para confirmar o impulso. |
| `ConvertLow` | `6` | Limite DI, o valor anterior deve ficar abaixo antes de uma nova negociação. |
| `AdxPeriod` | `10` | Período usado para o cálculo ADX. |
| `CandleType` | `H1` | Prazo que orienta o cálculo de Ichimoku. |
| `AdxCandleType` | `M1` | Prazo usado para monitoramento de ADX e DI. |

Todos os parâmetros são implementados com ajudantes `StrategyParam<T>`, permitindo otimização e ajustes de tempo de execução dentro do StockSharp Designer.

## Notas de implementação
- A conversão do pip segue a convenção forex padrão (0,0001 para cotações de 5 dígitos e 0,01 para instrumentos de 3 dígitos) para preservar os limites originais baseados em pip.
- Os valores ADX são armazenados em cache em `_latestPlusDi`, `_previousPlusDi`, `_latestMinusDi` e `_previousMinusDi`, garantindo que a verificação de aceleração DI corresponda às chamadas MQL `iADX` com turnos 0 e 1.
- `IsFormedAndOnlineAndAllowTrading()` bloqueia sinais até que a estratégia, os indicadores e os feeds de dados estejam prontos, evitando negociações prematuras durante o aquecimento.
- As entradas no mercado dependem de `Volume + Math.Abs(Position)` para que as mudanças de direção instantaneamente achatem as negociações existentes, emulando o comportamento de posição única do script MT4.
