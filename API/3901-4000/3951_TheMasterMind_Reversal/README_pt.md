# A estratégia de reversão MasterMind (StockSharp porta)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Porta do consultor especialista MetaTrader 4 "TheMasterMind" que combina um oscilador Stochastic com Williams %R para capturar reversões extremas.
- Implementado com StockSharp de alto nível API usando assinaturas de velas e ligações de indicadores.
- Negocia um único título e reage apenas às velas finalizadas, refletindo o estilo de execução original de “negociação no fechamento”.

## Lógica de negociação
1. **Preparação de indicadores**
   - `StochasticOscillator` fornece a linha de sinal %D com suavização %K/%D configurável e comprimento total de lookback.
   - `WilliamsR` mede a localização relativa do fechamento dentro da faixa máxima/mínima recente.
2. **Regras de entrada**
   - **Compre** quando `%D <= 3` _e_ `Williams %R <= -99.5`, sinalizando um extremo estocástico de sobrevenda juntamente com uma penetração profunda do WPR abaixo do limite inferior.
   - **Venda** quando `%D >= 97` _e_ `Williams %R >= -0.5`, sinalizando um extremo de sobrecompra confirmado por Williams %R permanecendo perto de 0.
   - Se existir uma posição oposta, ela é achatada primeiro e, em seguida, uma nova ordem de mercado é enviada com o volume base configurado.
3. **Regras de saída**
   - Os sinais reversos fecham a posição atual e mudam a direção (uma posição por vez, correspondendo ao modo desativado de hedge usado no script MQL).
   - Os serviços opcionais `StartProtection` de stop-loss, take-profit e trailing stop lidam com saídas de proteção exatamente uma vez por início de estratégia.

## Gestão de risco
- Os parâmetros `StopLoss`, `TakeProfit`, `UseTrailingStop`, `TrailingStop` e `TrailingStep` são mapeados para os controles de gerenciamento de dinheiro do EA original.
- Todas as distâncias são expressas em unidades de preço absoluto para permanecer independente do corretor. Deixe-os em `0` para desativar o respectivo recurso de proteção.
- `StartProtection` é ativado automaticamente quando pelo menos uma das distâncias de proteção é diferente de zero.

## Parâmetros de Estratégia
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `TradeVolume` | Tamanho base do lote para cada nova entrada. | `1` |
| `StochasticPeriod` | Lookback total para o oscilador estocástico. | `100` |
| `KPeriod` | %K comprimento de suavização. | `3` |
| `DPeriod` | %D comprimento do sinal. | `3` |
| `WilliamsPeriod` | Comprimento de lookback para Williams %R. | `100` |
| `StochasticBuyThreshold` | Limite superior que %D deve permanecer abaixo para permitir posições compradas. | `3` |
| `StochasticSellThreshold` | Limite inferior que %D deve permanecer acima para permitir vendas. | `97` |
| `WilliamsBuyLevel` | Nível de sobrevenda para Williams %R. | `-99.5` |
| `WilliamsSellLevel` | Nível de sobrecompra por Williams %R. | `-0.5` |
| `StopLoss` | Distância absoluta de stop-loss. | `0` |
| `TakeProfit` | Distância absoluta de lucro. | `0` |
| `UseTrailingStop` | Ativa a proteção de rastreamento quando `true`. | `false` |
| `TrailingStop` | Distância absoluta de parada final. | `0` |
| `TrailingStep` | Etapa aplicada durante o rastreamento. | `0` |
| `CandleType` | Prazo para a assinatura da vela principal (padrão 15 minutos). | `15m time frame` |

## Notas de implementação
- A estratégia assina uma única série de velas via `SubscribeCandles(CandleType)` e vincula os indicadores estocásticos e Williams %R usando `BindEx`.
- As decisões de negociação são tomadas somente quando `candle.State == CandleStates.Finished` e `IsFormedAndOnlineAndAllowTrading()` estão satisfeitos.
- Auxiliares de gráfico (`DrawCandles`, `DrawIndicator`, `DrawOwnTrades`) são invocados quando uma área do gráfico está disponível para visualizar os indicadores e negociações.
- As instruções de registro (`LogInfo`) espelham as strings de alerta originais, ajudando a rastrear o processo de decisão durante a negociação ao vivo ou backtesting.
