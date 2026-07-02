# Siga a estratégia de tendência de linha
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Follow Line é uma porta direta do MetaTrader consultor especialista `FollowLineEA_v1.0`. Ele replica a lógica original combinando um detector de rompimento de banda Bollinger com uma linha de tendência adaptativa que abraça a ação do preço. A estratégia escuta velas finalizadas e funciona em qualquer prazo fornecido pelo usuário.

Um rompimento acima da banda superior Bollinger eleva a linha de suporte abaixo do preço, enquanto um fechamento abaixo da banda inferior derruba uma linha de resistência sobre o preço. A linha desliza apenas na direção do rompimento, criando um padrão de escada que destaca tendências sustentadas. O preenchimento opcional ATR pode ampliar a linha para evitar que as posições sejam acionadas muito cedo. Filtros de momentum baseados em médias móveis confirmam as entradas dependendo do modo de seta selecionado.

## Lógica de negociação
1. **Cadeia de indicadores**
   - Bollinger Bandas (comprimento = `BollingerPeriod`, largura = `BollingerDeviations`).
   - ATR opcional (comprimento = `AtrPeriod`) para compensar a linha de tendência quando `UseAtrFilter` está ativado.
   - Uma família de médias móveis simples (comprimento = `MovingAveragePeriod`) aplicadas a preços máximos, mínimos, de abertura, de fechamento e medianos. Essas médias geram sinalizadores de confirmação quando `TypeOfArrows` é definido como `OpenCloseMedian` ou `HighLowOpenClose`.
2. **Atualização da linha de tendência**
   - Uma vela fechando acima da banda superior empurra a linha de tendência para a mínima da vela (menos o deslocamento de ATR se usado), mas nunca a reduz.
   - Uma vela fechando abaixo da banda inferior puxa a linha para a máxima da vela (mais ATR deslocamento, se usado), mas nunca a levanta.
   - A direção da linha de tendência define se o mercado é considerado altista (>0) ou baixista (<0).
3. **Sinais de entrada**
   - Quando a direção muda de baixa para alta e os filtros de seta concordam, uma seta de compra é colocada na fila.
   - Quando a direção muda de alta para baixa, uma seta de venda é enfileirada.
   - O parâmetro `IndicatorsShift` atrasa a execução para que a seta possa ser processada `IndicatorsShift` barras após ser formada, imitando a mudança do buffer MT4.
4. **Filtros de execução**
   - Filtro de tempo: as negociações são permitidas apenas entre `TimeStartTrade` e `TimeEndTrade` quando `UseTimeFilter` está ativado (a janela pode terminar à meia-noite).
   - Filtro de spread: se o spread atual exceder `MaxSpread` (medido em etapas de preço), os pedidos serão ignorados.
   - Limite de pedido: `MaxOrders` limita o tamanho absoluto da posição para replicar a verificação original de “pedidos máximos”.

## Gestão de risco
- **Sair no sinal oposto**: defina `CloseInSignal` como `true` para nivelar imediatamente a exposição existente quando a seta oposta disparar.
- **Bloqueios de cesta**: `CloseInProfit` e `CloseInLoss` fecham a posição atual assim que o alvo do pip especificado for atingido. `UseBasketClose` aplica os limites a toda a cesta em vez de separar a lógica longa e curta (espelha a implementação de MQL).
- **Stops e metas**: a estratégia chama `SetStopLoss`, `SetTakeProfit`, trailing e ponto de equilíbrio protegem cada barra quando as alternâncias correspondentes estão habilitadas (`UseStopLoss`, `UseTakeProfit`, `UseTrailingStop`, `UseBreakEven`). Todas as distâncias são expressas em etapas de preços.
- **Dimensionamento do lote**: quando `AutoLotSize` está ativado, o tamanho da posição é igual à parcela selecionada do valor atual do portfólio (`RiskFactor` por cento). Caso contrário, um `ManualLotSize` fixo será usado. O valor é normalizado para a etapa de volume do instrumento e limitado por limites cambiais.

## Parâmetros
| Grupo | Nome | Descrição |
| --- | --- | --- |
| Geral | `CandleType` | Prazo ou tipo de vela personalizado usado para assinatura. |
| Indicador | `BarsCount` | Profundidade histórica utilizada pelo indicador. |
| Indicador | `BollingerPeriod` / `BollingerDeviations` | Configuração Bollinger para detecção de breakout. |
| Indicador | `MovingAveragePeriod` | Comprimento das médias móveis que alimentam os filtros de seta. |
| Indicador | `AtrPeriod` / `UseAtrFilter` | ATR comprimento e sinalizador de ativação. |
| Indicador | `TypeOfArrows` | Modo de seta (`HideArrows`, `SimpleArrows`, `OpenCloseMedian`, `HighLowOpenClose`). |
| Indicador | `IndicatorsShift` | Atraso (em barras) entre a formação e a execução da flecha. |
| Hora | `UseTimeFilter`, `TimeStartTrade`, `TimeEndTrade` | Limites de sessão. |
| Filtros | `MaxSpread`, `MaxOrders` | Espalhe teto e limite de posição. |
| Risco | `CloseInSignal`, `UseBasketClose`, `CloseInProfit`, `PipsCloseProfit`, `CloseInLoss`, `PipsCloseLoss` | Regras de gerenciamento de cesta. |
| Risco | `UseTakeProfit`, `TakeProfit`, `UseStopLoss`, `StopLoss`, `UseTrailingStop`, `TrailingStop`, `TrailingStep`, `UseBreakEven`, `BreakEven`, `BreakEvenAfter` | Conjunto de ordens protetoras (valores em etapas de preços). |
| Gestão de dinheiro | `AutoLotSize`, `RiskFactor`, `ManualLotSize` | Dimensionamento de posição. |

## Notas de uso
- A estratégia funciona apenas em velas acabadas. Portanto, é seguro fazer backtest com a mesma compressão de barra da negociação ao vivo.
- A fila personalizada por trás de `IndicatorsShift` mantém o comportamento de alto nível API idêntico ao acesso ao buffer do indicador MT4 (`iCustom(..., shift)`).
- `TypeOfArrows = HideArrows` desativa a negociação enquanto preserva a lógica de desenho do indicador, exatamente como a fonte EA.
- Para visualizar as negociações, anexe a estratégia a uma área do gráfico após chamar `CreateChartArea()` (já tratado em `OnStarted`).

## Detalhes da conversão
- A lógica depende exclusivamente de indicadores StockSharp integrados e da assinatura de vela de alto nível API (sem buffer manual ou chamadas `GetValue`).
- O gerenciamento de pedidos é feito com `BuyMarket`/`SellMarket` mais os métodos auxiliares `SetStopLoss` e `SetTakeProfit`, espelhando o comportamento MT4 do código original.
- O dimensionamento de lote baseado em portfólio respeita os limites de troca por meio de verificações `VolumeStep`, `VolumeMin` e `VolumeMax` antes de enviar pedidos.
- A estratégia retém comentários de código em inglês e descrições de parâmetros para se alinhar com as diretrizes do repositório.
