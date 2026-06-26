# Estratégia de Exp Adaptive Renko MMRec Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o consultor especialista do MetaTrader 5 **Exp_AdaptiveRenko_MMRec_Duplex.mq5** para a API de alto nível do StockSharp. Dois fluxos independentes de Adaptive Renko — um configurado para oportunidades compradas e outro para vendidas — observam como os canais de tijolos personalizados alternam entre suporte e resistência. Quando o canal comprado reporta novo suporte enquanto o canal vendido perde resistência (ou vice-versa), a estratégia abre a posição de mercado correspondente. A versão C# mantém o bloco original de gestão monetária "MM Recounter" que reduz o tamanho da operação após uma série configurável de perdas e o restaura quando a sequência termina.

## Fluxo de trabalho principal

1. **Assinaturas de dados** – cada lado assina seu próprio tipo de vela (período) e vincula um indicador de volatilidade (ATR ou Desvio Padrão) através de `SubscribeCandles().BindEx(...)`. O indicador impulsiona a altura adaptativa do tijolo.
2. **Processamento de Adaptive Renko** – o helper `AdaptiveRenkoProcessor` reconstrói a lógica do indicador MQL, retornando um snapshot com a tendência mais recente e os níveis de suporte e resistência. Os sinais são avaliados apenas em velas concluídas.
3. **Lógica de entrada** – quando o snapshot do Renko comprado indica uma alta (suporte aparece na barra de sinal), a estratégia abre uma posição comprada. Entradas vendidas requerem uma baixa do fluxo vendido.
4. **Lógica de saída** – eventos de Renko opostos fecham uma posição ativa. Verificações adicionais aplicam distâncias de stop-loss e take-profit expressas em passos de preço.
5. **Gestão monetária MMRec** – cada direção mantém uma fila de valores de PnL realizados recentes. Se o número de perdas dentro da janela configurada atingir o gatilho de perdas, a próxima ordem usa o valor de gestão monetária reduzido (`LongSmallMoneyManagement` / `ShortSmallMoneyManagement`). Caso contrário, o valor normal (`LongMoneyManagement` / `ShortMoneyManagement`) é usado. O enum `MarginModeOption` reproduz os modos de dimensionamento MQL (lote, participação do saldo, participação baseada em perda, etc.).
6. **Registro de operações** – cada saída chama `RegisterTradeResult` para alimentar as filas MMRec. O corte da fila espelha as funções originais `BuyTradeMMRecounterS` e `SellTradeMMRecounterS` sem escanear o histórico do terminal.

## Grupos de parâmetros

| Grupo | Parâmetros principais | Descrição |
| --- | --- | --- |
| Lado comprado | `LongCandleType`, `LongVolatilityMode`, `LongVolatilityPeriod`, `LongSensitivity`, `LongPriceMode`, `LongMinimumBrickPoints`, `LongSignalBarOffset` | Controlam o fluxo de Adaptive Renko que produz entradas compradas. |
| Lado vendido | `ShortCandleType`, `ShortVolatilityMode`, `ShortVolatilityPeriod`, `ShortSensitivity`, `ShortPriceMode`, `ShortMinimumBrickPoints`, `ShortSignalBarOffset` | Espelham as configurações para o módulo vendido. |
| MMRec | `LongTotalTrigger`, `LongLossTrigger`, `LongSmallMoneyManagement`, `LongMoneyManagement`, `LongMarginMode`, `ShortTotalTrigger`, `ShortLossTrigger`, `ShortSmallMoneyManagement`, `ShortMoneyManagement`, `ShortMarginMode` | Replicam o bloco de recuperação de gestão monetária. Os parâmetros *TotalTrigger* definem o tamanho da janela deslizante, *LossTrigger* o número de perdas que ativa o volume reduzido. |
| Risco | `LongStopLossPoints`, `LongTakeProfitPoints`, `ShortStopLossPoints`, `ShortTakeProfitPoints`, `LongDeviationSteps`, `ShortDeviationSteps` | Expressam níveis de proteção e slippage informativo em passos de preço. |

## Notas de comportamento

- A estratégia funciona no modelo de conta netting: antes de abrir uma operação comprada, fecha qualquer vendida pendente e vice-versa.
- Os tamanhos de posição são calculados através de `CalculateVolume`. O helper suporta todos os modos de margem originais, incluindo dimensionamento baseado em perdas que depende da distância de stop-loss configurada.
- Todo o processamento de indicadores ocorre apenas em velas concluídas, respeitando o EA fonte.
- Os logs incluem o multiplicador de gestão monetária e o slippage esperado (em passos) para rastreabilidade.

## Arquivos

- `CS/ExpAdaptiveRenkoMmrecDuplexStrategy.cs` – implementação da estratégia com o processador Adaptive Renko e o módulo MMRec.
- `README.md` – documentação em inglês (este arquivo).
- `README_ru.md` – documentação em russo.
- `README_zh.md` – documentação em chinês.
