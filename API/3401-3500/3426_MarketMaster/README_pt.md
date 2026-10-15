# Estratégia Mestre de Mercado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

`MarketMasterStrategy` é uma conversão StockSharp de alto nível do MetaTrader 4 consultor especialista "Market Master" (`MQL/31326/MarketMaster EN.mq4`). O bot original combinou uma pilha rica de indicadores com regras intrincadas de gerenciamento de dinheiro, prevenção de notícias e pirâmides de pedidos em vários estágios. A porta C# concentra-se no núcleo técnico determinístico para que possa ser executado no mecanismo orientado a eventos do StockSharp sem quaisquer serviços da web externos. Todas as decisões dos indicadores são calculadas no período de negociação através de uma única assinatura de vela, de acordo com as diretrizes do repositório.

## Indicadores Principais

A estratégia vincula os seguintes indicadores StockSharp à série de velas de negociação:

- **AverageTrueRange (ATR)** – duas instâncias são mantidas. O primeiro rastreia as condições de entrada primárias, o segundo reflete o "hedge" MT4 ATR que foi usado para posições de recuperação.
- **MoneyFlowIndex (MFI)** – mede o fluxo de preços ajustado pelo volume para detectar oscilações de acumulação ou distribuição.
- **BullsPower / BearsPower** – replica os filtros MT4 `iBullsPower` e `iBearsPower` que exigiam domínio de alta/baixa antes de realizar negociações.
- **StochasticOscillator** – fornece `%K` e `%D` linhas. A conversão respeita os comprimentos originais do oscilador e permite ao usuário ativar ou desativar o filtro.
- **ParabolicSar** – dois períodos de tempo foram usados em MetaTrader. A porta StockSharp mantém dois indicadores SAR independentes (primário e de confirmação) cujas etapas refletem as entradas do consultor especialista.

Todos os indicadores são aquecidos automaticamente por StockSharp. A estratégia não acessa o histórico do indicador por meio de `GetValue` – em vez disso, ela armazena os valores anteriores dentro de campos privados (`_prevAtr`, `_prevMfi`, `_prevStochasticMain`, etc.) conforme exigido pelas regras de conversão.

## Lógica de Sinais

O especialista MQL definiu duas famílias de entradas principais ("ZERO" e "MA"). Eles compartilham filtros ATR/MFI/Bulls/Bears idênticos, mas diferem na confirmação do oscilador. A versão StockSharp expõe o ramo "MA" mais rico porque é o mais restritivo e, portanto, mais próximo das condições reais de negociação. Um sinal longo é confirmado quando todas as afirmações a seguir são verdadeiras em uma vela finalizada:

1. ATR está subindo em relação à vela anterior (seja a primária ATR ou a cobertura ATR dependendo se já existe uma posição).
2. A IMF está subindo e o Bears Power está positivo, sinalizando pressão de alta.
3. O oscilador Stochastic está ativado e `%K` está acima de `%D`, com tendência de alta, enquanto `%K` permanece abaixo do teto de sobrecompra configurável (`StochasticBuyLevel`).
4. Os filtros Parabolic SAR são ativados e a vela fecha acima de ambos os valores SAR.
5. O volume atual da vela atende ao limite configurado (`MinVolume` ou `MinHedgeVolume`).

Os sinais curtos refletem a lógica longa com IMF decrescente, Bulls Power negativo, `%K` abaixo de `%D` e SAR valores acima do preço. As verificações de volume evitam negociações durante mercados fracos, replicando as chamadas `iVolume` do MT4.

## Gerenciamento de posição

- **Volume automático** – o EA original oferecia um bloco de dimensionamento de posição baseado em equilíbrio. `CalculateBaseVolume` segue o mesmo espírito ao dimensionar o volume do pedido com `RiskMultiplier` enquanto respeita as restrições `VolumeStep`, `MinVolume` e `MaxVolume` do instrumento.
- **Pirâmide** – quando `AllowSameSignalEntries` é `true`, pedidos adicionais reutilizam o volume base multiplicado por `VolumeMultiplier`. Como as estratégias StockSharp funcionam com posições líquidas, a pirâmide aumenta a exposição líquida longa ou curta em vez de abrir tickets paralelos.
- **Sinais opostos** – `AllowOppositeEntries` controla se uma reversão detectada fecha imediatamente a posição atual e, opcionalmente, abre uma negociação na nova direção. Quando desativada, a estratégia sai, mas espera por um novo sinal antes de entrar novamente, imitando a alternância "Sem sinal oposto" na interface MT4.
- **Stop-loss** – a entrada MT4 `StopLoss` é exposta como `StopLossPoints`. Se o instrumento fornecer um `PriceStep`, o valor será convertido em StockSharp ordens de proteção por meio de `StartProtection`.
- **Horário de negociação** – `UseTradingWindow`, `TradingStart`, `TradingEnd`, `UseTradingBreak`, `BreakStart` e `BreakEnd` reproduzem a janela de abertura e a pausa intradiária do especialista de origem. As comparações de tempo são realizadas no fuso horário de troca transportado pelas mensagens de vela recebidas.

## Diferenças em relação à versão MetaTrader

- **Filtros de notícias** – o robô MT4 baixou dados do calendário econômico do Investing.com e DailyFX. A conversão omite todas as chamadas de rede e as substitui pelo controle manual da janela de negociação. Para comportamentos sensíveis às notícias, ajuste os parâmetros de tempo ou pause a estratégia externamente.
- **Verificações do histórico de pedidos** – funções como `OrdersHistoryTotal()` e lógica "abrir novamente" foram fortemente acopladas ao modelo de ticket de MetaTrader. StockSharp funciona com uma posição líquida, então a porta simplesmente permite a reentrada quando o filtro de direção se tornar válido novamente.
- **Pedidos de recuperação** – o código original gerenciava vários Números Mágicos e rótulos de comentários. A porta mantém a lógica do multiplicador (`VolumeMultiplier`), mas cada pedido adicional modifica a posição líquida única.
- **Trailing stop** – O bloco `TrailingStop`/`TrailingStep` de MetaTrader dependia de modificação de ordem assíncrona. Os usuários de StockSharp podem estender a estratégia assinando eventos `PositionChanged` ou ativando opções de rastreamento em `StartProtection`, mas a conversão de linha de base se concentra na paridade do sinal.

## Parâmetros

| Propriedade | Padrão | Descrição |
| --- | --- | --- |
| `OrderVolume` | `1` | Tamanho base do pedido quando o volume automático está desativado. |
| `UseAutoVolume` | `true` | Habilite o escalonamento de volume baseado em risco. |
| `RiskMultiplier` | `10` | Porcentagem do saldo do portfólio utilizado no cálculo automático de volume (espelhos `Risk_Multiplier`). |
| `VolumeMultiplier` | `2` | Fator de pirâmide para entradas adicionais (`KLot`). |
| `MinVolume` | `3000` | Volume mínimo de velas para a primeira entrada (`MinVol`). |
| `MinHedgeVolume` | `3000` | Limite de volume para negociações complementares (`MinVolH`). |
| `AtrPeriod` / `AtrHedgePeriod` | `14` | ATR comprimentos para os filtros de base e de hedge. |
| `MfiPeriod` | `14` | Período das IMFs. |
| `BullBearPeriod` | `14` | Período de poder de touros/ursos. |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | `5 / 3 / 3` | Configuração do oscilador Stochastic. |
| `StochasticBuyLevel` / `StochasticSellLevel` | `60 / 40` | Limites do oscilador (`StoBuy` e `StoSell`). |
| `UseStochasticFilter`, `UsePsarFilter`, `UsePsarConfirmation` | `true` | Alterna para confirmações baseadas em indicadores. |
| `PsarStep` / `PsarMaxStep` / `PsarConfirmStep` / `PsarConfirmMaxStep` | `0.02 / 0.2 / 0.02 / 0.2` | SAR acelerações e limites. |
| `AllowSameSignalEntries` | `false` | Habilite a pirâmide em sinais idênticos. |
| `AllowOppositeEntries` | `true` | Permitir negociações de reversão imediata. |
| `UseTradingWindow` | `false` | Restrinja a negociação a um intervalo de tempo. |
| `TradingStart` / `TradingEnd` | `06:00 / 18:00` | Janela de negociação diária. |
| `UseTradingBreak` | `false` | Habilite um pequeno intervalo intradiário. |
| `BreakStart` / `BreakEnd` | `06:00:01 / 06:00:02` | Quebre limites (corresponda aos padrões MT4). |
| `StopLossPoints` | `0` | Parada de proteção opcional nos pontos do instrumento. |
| `CandleType` | `15m TimeFrame` | Série de velas usada para todos os indicadores. |

## Notas de uso

1. Anexe a estratégia a um título e portfólio no StockSharp Designer ou em código e, em seguida, inicie-a durante as horas de aquecimento para permitir a formação de todos os indicadores.
2. Se você precisar de confirmação de vários períodos, ajuste `CandleType` e as configurações de SAR adequadamente. A estratégia assina um único feed de vela e vincula todos os indicadores por meio de `Bind`, portanto, nenhum registro manual do indicador é necessário.
3. Use o registro StockSharp (`LogInfo`, `LogWarning`) para depuração se você estender o código. A conversão mantém o gerenciamento de estado interno simples para que módulos adicionais (por exemplo, proteção final) possam ser facilmente conectados.
4. A estratégia é baseada na posição líquida. Se você planeja modelar o comportamento de tickets individuais semelhante a MetaTrader, envolva a estratégia em um roteador de segurança múltipla que rastreie tickets sintéticos.

## Ampliando o Porto

- Implemente a lógica de saída personalizada substituindo `OnNewMyTrade` ou inscrevendo-se em `PositionChanged`.
- Adicione integração de calendário econômico introduzindo um componente externo que alterna `UseTradingWindow` ou chama `Stop()` quando eventos de alto impacto se aproximam.
- Para visualização do sinal, chame `CreateChartArea()` e `DrawIndicator()` em `OnStarted` – a conversão deixa esses ganchos vazios para maior clareza.

O código é totalmente compatível com as diretrizes do repositório: ele usa recuo de tabulação, assinaturas `Bind` de alto nível, evita referências anteriores de indicadores e expõe todas as entradas configuráveis por meio de objetos `StrategyParam`.
