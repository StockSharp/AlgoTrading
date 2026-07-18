# Estratégia TDS Global Pending (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia porta o consultor especialista do MetaTrader 5 **TDSGlobal** de `MQL/23255/TDSGlobal.mq5` para a API de alto nível do StockSharp. Avalia o Momentum em velas de quatro horas através da linha MACD, do histograma MACD (OsMA) e do Índice de Força. Quando a combinação de indicadores sinaliza uma possível reversão, a estratégia envia ordens limite pendentes em torno dos extremos da vela anterior e gerencia a posição resultante com lógica opcional de stop-loss, take-profit e trailing-stop.

A implementação reproduz o fluxo de trabalho original adaptando-o a construções idiomáticas do StockSharp como `StrategyParam<T>`, assinaturas de velas via `SubscribeCandles` e tratamento assíncrono de ordens através dos eventos do ciclo de vida da estratégia.

## Lógica de trading

1. **Cálculos de indicadores**
   - `MACD(12, 26, 9)` fornece tanto a linha MACD quanto o histograma (OsMA).
   - `ForceIndex(24)` mede a força da última vela concluída.
   - Cada indicador é atualizado no fechamento do tipo de vela selecionado (padrão: 4 horas).
2. **Detecção de sinais**
   - O algoritmo aguarda até que dois valores históricos de MACD e OsMA estejam disponíveis para determinar sua inclinação.
   - Uma configuração de *venda* requer que OsMA aumente (`osma[1] > osma[2]`) enquanto o Índice de Força da vela anterior seja negativo.
   - Uma configuração de *compra* requer que OsMA diminua (`osma[1] < osma[2]`) enquanto o Índice de Força anterior seja positivo.
3. **Colocação de ordens**
   - Ordens limite de venda são colocadas ligeiramente acima do máximo da vela anterior; ordens limite de compra ligeiramente abaixo do mínimo da vela anterior.
   - Se o preço não estiver suficientemente longe do bid/ask atual, o preço da ordem é puxado para o buffer de offset configurado (`EntryOffsetPips`, padrão 16 pips).
   - A estratégia verifica se a distância entre o preço da ordem e o bid/ask atual excede a aproximação do nível de segurança do corretor (`MinDistancePips` ou o valor dinâmico baseado no spread).
4. **Controles de risco**
   - Níveis opcionais de stop-loss e take-profit são calculados a partir do preço da ordem.
   - Quando uma posição está ativa, um trailing stop pode avançar pelo passo configurado quando o preço se move além da distância de trailing inicial.
   - Se o preço atingir os níveis de proteção dentro de uma vela, a posição é fechada com uma ordem de mercado para imitar o comportamento do MetaTrader.
5. **Manutenção de ordens**
   - Ordens pendentes são canceladas quando a inclinação do OsMA se vira contra a configuração original, correspondendo à rotina de limpeza do EA fonte.
   - O preenchimento de um lado cancela automaticamente a ordem pendente oposta para evitar exposições conflitantes.

## Gestão de capital

Duas abordagens de dimensionamento de posição estão disponíveis:

- **Volume fixo** (padrão `OrderVolume = 1`) — usa o `Strategy.Volume` base sem ajustes.
- **Dimensionamento baseado em risco** — quando `UseRiskSizing` está habilitado, a estratégia estima o patrimônio líquido do portfólio, converte o percentual de risco configurado em risco em moeda e divide pelo distância do stop-loss para derivar o volume da ordem. Os volumes são alinhados ao passo de volume do instrumento para evitar tamanhos de ordem inválidos.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `OrderVolume` | Tamanho fixo de ordem quando o dimensionamento por risco está desabilitado. | 1 |
| `UseRiskSizing` | Habilitar gestão de capital baseada em `RiskPercent`. | true |
| `RiskPercent` | Percentual do patrimônio do portfólio arriscado por operação. | 3 |
| `MacdFastPeriod` | Comprimento da EMA rápida para a linha MACD. | 12 |
| `MacdSlowPeriod` | Comprimento da EMA lenta para a linha MACD. | 26 |
| `MacdSignalPeriod` | Comprimento da EMA de sinal para o histograma MACD. | 9 |
| `ForceLength` | Comprimento de suavização EMA para o Índice de Força. | 24 |
| `StopLossPips` | Distância do stop-loss em pips (0 desabilita). | 50 |
| `TakeProfitPips` | Distância do take-profit em pips (0 desabilita). | 50 |
| `TrailingStopPips` | Distância do trailing stop em pips (0 desabilita). | 5 |
| `TrailingStepPips` | Passo mínimo para atualizações de trailing. | 5 |
| `EntryOffsetPips` | Buffer adicionado em torno de máximos/mínimos anteriores para ordens pendentes. | 16 |
| `MinDistancePips` | Distância mínima permitida entre o preço e os níveis de proteção. | 3 |
| `PipSize` | Tamanho do pip usado para conversões pip-para-preço. | 0.0001 |
| `CandleType` | Tipo de vela processado pela estratégia. | Velas de 4 horas |

## Notas de uso

1. Adicione o arquivo `CS/TdsGlobalPendingStrategy.cs` ao seu projeto StockSharp ou carregue-o dinamicamente através do ambiente do Backtester.
2. Atribua o instrumento e portfólio desejados antes de iniciar a estratégia. Se `UseRiskSizing` estiver habilitado, certifique-se de que o portfólio forneça valores de patrimônio atuais.
3. A estratégia requer pelo menos duas velas concluídas para inicializar as inclinações de MACD/OsMA. Espere uma breve fase de aquecimento.
4. Monitore os logs para eventos detalhados de ordens e posições. A implementação registra ações-chave (envio de ordens, cancelamento, atualizações de trailing) para facilitar a verificação em relação ao comportamento original do EA.

## Diferenças da versão MQL

- A API de alto nível gerencia eventos de ordens assíncronas, portanto os preenchimentos de ordens limite são tratados via `OnOwnTradeReceived` em vez de resultados síncronos de `OrderSend`.
- Os níveis de "congelamento" e "stops" do corretor são aproximados usando a distância mínima configurada e uma heurística baseada no spread, pois o StockSharp não expõe limites de negociação específicos do MetaTrader.
- Saídas de proteção são executadas via ordens de mercado quando a vela mostra uma ruptura. Isso replica a lógica de modificação manual de stop do EA sem depender das restrições do servidor de negociação MT5.

Esses ajustes mantêm a lógica de negociação fiel enquanto garantem que a estratégia se integre suavemente ao framework StockSharp.
