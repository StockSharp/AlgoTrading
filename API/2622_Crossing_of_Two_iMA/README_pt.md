# Estratégia de Cruzamento de Dois iMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o clássico consultor especialista MetaTrader 5 **"Crossing of two iMA"** para a API de alto nível do StockSharp. Opera quando duas médias móveis configuráveis se cruzam e pode opcionalmente exigir confirmação de uma terceira média móvel que atua como filtro direcional. A implementação mantém a flexibilidade original ao suportar dimensionamento de posição manual ou baseado em risco, offsets no estilo de entrada pendente e um trailing stop com passo definido pelo usuário.

A conversão processa sinais no fechamento de cada candle finalizado, replicando como o expert MQL5 aguarda uma nova barra. O comportamento de ordens pendentes (`PriceLevelPips`) é simulado internamente monitorando máximas e mínimas dos candles, portanto nenhuma ordem stop/limite real é enviada. Um trigger pendente comprado é ativado quando a barra atinge o preço escolhido para entradas buy stop ou cai ao preço para entradas buy limit, e a mesma lógica simétrica é aplicada para configurações vendidas.

## Regras de negociação

- **Indicadores**
  - Média móvel `First` (período, deslocamento e método são configuráveis).
  - Média móvel `Second` (também totalmente configurável).
  - Média móvel `Third` opcional usada como filtro (`UseThirdMovingAverage = true`).
- **Critérios de entrada**
  - **Cruzamento primário (barras 0 e 1)**
    - **Comprado**: a primeira MA cruza acima da segunda MA na barra atual enquanto estava abaixo na barra anterior. Se o filtro estiver ativo, a terceira MA deve permanecer abaixo da primeira MA para validar o rompimento comprado.
    - **Vendido**: a primeira MA cruza abaixo da segunda MA e, se o filtro estiver habilitado, a terceira MA deve permanecer acima da primeira MA.
  - **Cruzamento de reserva (barras 0 e 2)**
    - Realiza uma pesquisa adicional retroativa para capturar cruzamentos rápidos ocorridos entre as duas barras anteriores. A estratégia ignora este sinal se outra operação já foi aberta nas últimas três barras (igual à pesquisa de histórico do MQL5).
- **Direção**: tanto comprado quanto vendido.
- **Stops e alvos**
  - Stop loss e take profit são expressos em pips. São convertidos em offsets de preço baseados no tamanho do tick do instrumento e ajustados para precificação de 3/5 dígitos igual ao EA original.
  - O trailing stop ativa apenas quando `TrailingStopPips > 0`. Move o stop pela distância de trailing assim que o preço avança pelo menos `TrailingStepPips` além do nível de stop anterior.
- **Modo de ordem pendente (`PriceLevelPips`)**
  - `0`: entrar imediatamente a mercado.
  - `< 0`: simular ordens stop (buy stop acima do preço, sell stop abaixo do preço). O stop loss e take profit são deslocados pelo mesmo offset.
  - `> 0`: simular ordens limite (buy limit abaixo do preço, sell limit acima). Os níveis de proteção são deslocados de acordo.

## Gestão de capital

- `UseFixedVolume = true` replica o modo de lote manual do EA. A estratégia simplesmente usa `Volume` (e fecha posições opostas antes de abrir uma nova).
- Quando `UseFixedVolume = false`, a estratégia aloca risco como `Portfolio.CurrentValue * RiskPercent / 100`. O tamanho da ordem torna-se `riskAmount / stopDistance`. Se nenhum stop loss for fornecido (`StopLossPips = 0`), a distância de risco calculada é zero, portanto a estratégia se recusa a abrir uma posição — idêntico ao comportamento original de `MoneyFixedRisk` retornando zero lotes.

## Lógica de trailing

- As posições compradas fazem trailing do stop para `Close - TrailingStopPips * pipValue` assim que o preço avançou pelo menos `TrailingStepPips` além do stop anterior. O valor de trailing sempre se move para cima e nunca afrouxa o stop.
- As posições vendidas espelham esse comportamento movendo o stop para `Close + TrailingStopPips * pipValue` quando o preço avança o suficiente a seu favor.
- Take profit e stop inicial são verificados antes dos ajustes de trailing, garantindo que as saídas correspondam às prioridades do EA original.

## Parâmetros padrão

- Primeira MA: comprimento `5`, deslocamento `3`, método `Smoothed`.
- Segunda MA: comprimento `8`, deslocamento `5`, método `Smoothed`.
- Filtro de terceira MA: habilitado, comprimento `13`, deslocamento `8`, método `Smoothed`.
- Controles de risco: stop loss `50` pips, take profit `50` pips, trailing `10` pips com passo de `4` pips.
- Gestão de capital: `UseFixedVolume = true`, `RiskPercent = 5` para o modo de dimensionamento alternativo.
- Offset pendente: `0` pips (execução a mercado).
- Tipo de candle: período de 1 minuto (pode ser alterado para corresponder ao período do gráfico original).

## Notas de implementação

- Os parâmetros `shift` da média móvel atrasam os valores de sinal exatamente pelo número configurado de barras, de modo que o traçado nos gráficos StockSharp corresponde ao deslocamento visual do MT5.
- A estratégia armazena apenas o estado mínimo necessário (atual, anterior e duas barras atrás) para satisfazer a lógica "barras [0], [1], [2]" do MQL5. Nenhuma coleção histórica é recriada além desse buffer.
- As entradas pendentes são limpas quando um novo sinal aparece, replicando a chamada `DeleteAllOrders()` do EA.
- Como o StockSharp executa ordens de forma assíncrona, o preço de entrada registrado para cálculos de trailing e alvo usa o preço de trigger pretendido. Os backtests, portanto, reproduzem a lógica do EA original em dados de candles sem depender de preenchimentos no nível de tick.
