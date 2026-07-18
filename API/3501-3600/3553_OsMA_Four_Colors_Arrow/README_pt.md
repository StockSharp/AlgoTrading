# Estratégia de seta de quatro cores OsMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia recria o comportamento do consultor especialista MetaTrader "OsMA Four Colors Arrow" dentro da estrutura StockSharp. O EA original reage às setas coloridas produzidas pelo indicador que o acompanha sempre que o OsMA (MACD histograma) muda de fase. Na versão StockSharp, o mesmo comportamento é modelado monitorando cruzamentos zero do histograma MACD: uma linha de alta (o histograma se move de negativo para positivo) aciona entradas longas, enquanto uma linha de baixa aciona entradas curtas. O modo reverso opcional vira a lógica de cabeça para baixo para testes de hedge ou de reversão à média.

O modelo funciona apenas com velas finalizadas e pode impor uma sessão de negociação diária semelhante ao filtro de tempo oferecido pela versão MQL. O gerenciamento de dinheiro integrado inclui volume de negociação configurável, um limite para o número de posições agregadas e proteção automatizada de stop-loss/take-profit/trailing expressa em pips.

## Lógica de negociação

1. Assine o período de tempo selecionado e calcule um histograma MACD (OsMA) usando comprimentos configuráveis de sinal rápido, lento e EMA.
2. Quando uma vela fecha, verifique o sinal do histograma:
   - Histograma cruzando acima de zero → seta de alta → sinal de compra.
   - Histograma cruzando abaixo de zero → seta de baixa → sinal de venda.
3. Aplique recursos opcionais antes de enviar um pedido:
   - Filtro de direção (somente longo, somente curto ou ambos).
   - Modo reverso para inverter sinais.
   - Feche a exposição oposta existente antes de abrir a nova negociação.
   - Limite a uma posição ativa ou acumule até a exposição máxima configurada.
4. As ordens de mercado são enviadas com o tamanho de lote configurado. `StartProtection` converte entradas de pip em compensações de preços absolutos para executar o gerenciamento de stop-loss, take-profit e trailing automaticamente.
5. As negociações são ignoradas fora da sessão intradiária permitida quando o filtro de tempo está habilitado.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `CandleType` | Prazo usado para cálculos e geração de sinal. |
| `FastPeriod` / `SlowPeriod` / `SignalPeriod` | EMA comprimentos para o histograma MACD (OsMA). |
| `StopLossPips` / `TakeProfitPips` | Metas de risco em pips. Defina como zero para desativar. |
| `TrailingActivatePips` | Lucro (em pips) necessário antes que o trailing stop possa se mover. |
| `TrailingStopPips` | Distância final em pips. Zero desativa o módulo final. |
| `TrailingStepPips` | Pips extras que devem ser ganhos antes de apertar o trailing stop novamente. |
| `MaxPositions` | Unidades máximas de posição agregada (`TradeVolume` múltiplos). Zero significa ilimitado. |
| `ReverseSignals` | Inverta a direção de entrada (compra ↔ venda). |
| `DirectionMode` | Restrinja os sinais para somente longos, somente curtos ou ambos. |
| `CloseOppositePositions` | Feche qualquer exposição oposta antes de agir no novo sinal. |
| `OnlyOnePosition` | Se `true`, impede a adição a uma posição já aberta na mesma direção. |
| `UseTimeControl` | Habilite o filtro da sessão de negociação intradiária. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Limites da sessão (o final pode ser anterior ao início para cobrir sessões noturnas). |
| `TradeVolume` | Volume do pedido em lotes. |

## Notas

- As entradas de parada móvel imitam o EA: o rastreamento fica disponível somente após `TrailingActivatePips` e se move em etapas definidas por `TrailingStepPips`.
- A estratégia exige que o título tenha `PriceStep` e `Decimals` válidos para converter pips em compensações de preço. Os padrões voltam para uma unidade de preço absoluto se o instrumento não os fornecer.
- Se `MaxPositions` for maior que um, a estratégia pode ser gradualmente ampliada adicionando repetidamente `TradeVolume` respeitando o limite máximo de exposição.
- Quando `UseTimeControl` está ativado e os horários de início e término coincidem, a negociação é desativada para evitar sessões ambíguas.
- A lógica atua apenas em velas fechadas; não há envio de pedido intra-barra, correspondendo ao comportamento do modelo MQL.
