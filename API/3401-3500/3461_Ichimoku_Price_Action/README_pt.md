# Ichimoku Estratégia de Ação de Preço
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Ichimoku Estratégia de Ação de Preço** é um sistema de impulso MACD filtrado por tempo, portado do MQL4 especialista "Ichimoku Estratégia de Ação de Preço v1.0" para o StockSharp API de alto nível. O EA original abria ordens de mercado sempre que a negociação era habilitada para o instrumento e o filtro MACD opcional confirmava a direção. Esta porta C# mantém a mesma ideia, ao mesmo tempo que fornece controles de risco detalhados para colocação de stop-loss, tratamento de ponto de equilíbrio e saídas finais.

A estratégia foi projetada para traders discricionários que desejam automatizar um jogo direcional no horário do dia com dependências mínimas de indicadores. Todos os sinais de negociação são avaliados em velas concluídas do período de negociação escolhido, ao mesmo tempo em que suportam prazos auxiliares para ATR- e paradas de proteção baseadas em oscilações.

> **Importante:** A versão StockSharp mantém no máximo uma posição líquida por vez. A exposição simultânea de compra/venda simultânea no estilo hedge do modelo original não é suportada porque o StockSharp `Strategy` opera em posições líquidas. Todos os outros recursos de gerenciamento de dinheiro são expressos por meio de lógica stop, target e trailing executada em cada vela finalizada.

## Lógica de negociação
1. **Filtro de sessão** – As entradas são permitidas somente quando o horário atual estiver dentro da janela `[StartTime; EndTime]`. Definir ambos os parâmetros como `00:00` desativa o filtro de sessão.
2. **MACD confirmação (opcional)** – Quando `UseMacdFilter = true`, as posições compradas exigem MACD linha principal acima da linha de sinal, as posições vendidas exigem o oposto. As configurações de MACD são totalmente configuráveis.
3. **Colocação de ordem** – Se a negociação estiver habilitada para uma direção e nenhuma posição estiver aberta, a estratégia envia uma ordem de mercado com o `Volume` configurado.
4. **Paradas de proteção** – Dependendo de `StopLossMode`, a parada inicial é colocada usando uma distância fixa de pip, um múltiplo de ATR ou o último balanço extremo coletado de um período de tempo inferior. O stop é recalculado em cada vela e reduzido quando o nível recém-calculado é mais conservador.
5. **Metas** – Uma meta fixa de pip ou uma meta dinâmica de risco/recompensa com base no stop ativo é verificada a cada vela. Uma vez alcançada, a posição é fechada no mercado.
6. **Break-even e trailing** – Quando o lucro não realizado atinge `MoveToBreakEven`, o stop é puxado para o preço de entrada. Após `TrailingTrigger` pips de lucro, o módulo de rastreamento é ativado e continua pressionando o stop toda vez que o preço melhora em `TrailingStep` pips, enquanto mantém uma distância de `TrailingStop` pips do fechamento da vela.
7. **Saída reversa** – Se `CloseOnReverse = true`, qualquer sinal de entrada oposto fecha imediatamente a posição atual antes de potencialmente virar na nova direção.

## Gestão de risco
- **Parar perda**
  - *Pips fixos* – Usa `StopLossPips` multiplicado pela etapa de preço do instrumento.
  - *multiplicador ATR* – Usa o valor ATR mais recente de `AtrCandleType` multiplicado por `AtrMultiplier`.
  - *Swing high/low* – Usa o swing extremo mais recente calculado por `SwingCandleType` com lookback de `SwingBars`.
- **Receba lucro**
  - *Pips fixos* – Usa `TakeProfitPips`.
  - *Risco/Recompensa* – Usa a distância de parada atual multiplicada por `TakeProfitRatio`.
- **Ponto de equilíbrio** – `MoveToBreakEven` define quantos pips lucrativos são necessários antes que o stop seja bloqueado no preço de entrada.
- **Trailing** – Controlado por `TrailingStop`, `TrailingTrigger` e `TrailingStep` para manter os lucros assim que o mercado se mover favoravelmente.

## Parâmetros
| Grupo | Nome | Descrição |
| --- | --- | --- |
| Geral | `BuyMode` | Permitir entradas longas. |
| Geral | `SellMode` | Permitir entradas curtas. |
| Geral | `CandleType` | Prazo de negociação (padrão 1 hora). |
| Cronograma | `StartTime` / `EndTime` | Janela de sessão no horário de troca (00:00 → desabilitada). |
| Filtros | `UseMacdFilter` | Ative a confirmação MACD. |
| Filtros | `MacdFast`, `MacdSlow`, `MacdSignal` | MACD períodos para EMA rápida, EMA lenta e sinal EMA. |
| Risco | `StopLossMode` | Cálculo de stop-loss: `FixedPips`, `AtrMultiplier`, `SwingHighLow`. |
| Risco | `StopLossPips` | Distância em pips quando o modo fixo é selecionado. |
| Risco | `AtrMultiplier`, `AtrPeriod`, `AtrCandleType` | Configuração de parada baseada em ATR. |
| Risco | `SwingBars`, `SwingCandleType` | Configuração de parada oscilante alta/baixa. |
| Risco | `TakeProfitMode` | Modo de destino: `FixedPips` ou `RiskReward`. |
| Risco | `TakeProfitPips`, `TakeProfitRatio` | Distâncias alvo. |
| Risco | `CloseOnReverse` | Feche a posição ativa quando o sinal oposto aparecer. |
| Pedidos | `Volume` | Volume de ordens de mercado (lotes/contratos). |
| Risco | `MoveToBreakEven` | Limite de lucro (em pips) para mover o stop para a entrada. |
| Risco | `TrailingStop`, `TrailingTrigger`, `TrailingStep` | Configuração do trailing stop em pips. |

## Notas de uso
- Certifique-se de que o instrumento tenha `PriceStep` definido; caso contrário, a estratégia assume um tamanho de pip de `0.0001`.
- Quando ATR ou paradas oscilantes estão habilitadas, as assinaturas auxiliares correspondentes são adicionadas automaticamente. Certifique-se de que o feed de dados forneça esses prazos.
- Se você precisar desativar o ponto de equilíbrio ou o comportamento final, defina os parâmetros correspondentes como `0`.
- A estratégia é neutra por padrão na sessão aberta. Não empilhará várias posições na mesma direção; as reentradas acontecem somente após o fechamento da negociação anterior.

## Limitações em comparação com a versão MQL
- Somente posições líquidas são suportadas (limitação StockSharp). As negociações simultâneas de compra e venda no estilo hedge não são reproduzidas.
- Modos de gerenciamento de dinheiro, como dimensionamento de Kelly ou realização parcial de lucros, não fazem parte desta porta.
- A confirmação manual, os gráficos do painel e os recursos de captura de tela do modelo MQL são omitidos intencionalmente.

## Lista de verificação de backtesting
1. Configure os prazos `CandleType` e auxiliares desejados.
2. Ajuste os parâmetros `Volume` e stop/target para corresponder às configurações originais do EA.
3. Ative ou desative a confirmação MACD dependendo do uso do modelo.
4. Execute a simulação garantindo que a janela da sessão de negociação corresponda aos seus testes originais.
5. Revise as mensagens de log geradas para confirmar que os eventos de parada e destino acontecem conforme esperado.
