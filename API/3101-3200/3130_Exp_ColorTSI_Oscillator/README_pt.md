# Estratégia Exp Color TSI Oscilador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
- Conversão do consultor especializado MetaTrader 5 **Exp_ColorTSI-Oscillator** para o framework StockSharp.
- Reconstrói o oscilador ColorTSI: um True Strength Index de dupla suavização com uma linha de trigger atrasada e múltiplos algoritmos de suavização retirados de `SmoothAlgorithms.mqh`.
- Gera trades quando o oscilador sobe ou desce em relação ao seu trigger atrasado, replicando o estilo de "reversão de oscilação" usado pelo EA original.

## Reconstrução do indicador
- O preço aplicado é selecionado através da opção `ColorTsiAppliedPrice` (fechamento, abertura, mediana, típico, ponderado, Demark, etc.).
- O momentum de preço (`diff = price[n] - price[n-1]`) e seu valor absoluto são suavizados em dois estágios:
  1. **Primeiro estágio**: `ColorTsiSmoothingMethod` configurável (`Sma`, `Ema`, `Smma`, `Lwma`, `Jjma`, `Jurx`, `Parma`, `T3`, `Vidya`, `Ama`) com comprimento `FirstLength` e fase `FirstPhase` para filtros do tipo Jurik.
  2. **Segundo estágio**: opções de método idênticas com `SecondLength`/`SecondPhase` aplicadas à série de momentum já suavizada.
- A saída do oscilador é `TSI = 100 * smoothMomentum / smoothAbsMomentum`. Quando o denominador é zero, o valor é ignorado.
- Uma linha de trigger é obtida atrasando o TSI por `TriggerShift` barras, espelhando a lógica do buffer do MetaTrader.
- Os valores históricos são armazenados para que `SignalBar` corresponda ao padrão de acesso `CopyBuffer` do MetaTrader (índice `SignalBar` = barra fechada mais recente examinada, `SignalBar + 1` = barra anterior, etc.).

## Regras de negociação
- Os cálculos são executados em candles terminados fornecidos por `CandleType` (padrão: período de 4 horas).
- Seja `TSI[k]` o valor do oscilador e `Trigger[k]` a série atrasada.
- **Contexto de alta**: `TSI[SignalBar + 1] > Trigger[SignalBar + 1]` ⇒ a barra anterior mostrou momentum ascendente.
  - Fechar vendidos se `EnableShortExits` for true.
  - Abrir uma posição comprada quando `EnableLongEntries` for true **e** `TSI[SignalBar] ≤ Trigger[SignalBar]`, sinalizando uma oscilação ascendente após o recuo.
- **Contexto de baixa**: `TSI[SignalBar + 1] < Trigger[SignalBar + 1]` ⇒ a barra anterior mostrou momentum descendente.
  - Fechar comprados se `EnableLongExits` for true.
  - Abrir uma posição vendida quando `EnableShortEntries` for true **e** `TSI[SignalBar] ≥ Trigger[SignalBar]`.
- Os sinais de entrada são codificados pelo tempo da barra analisada mais um período completo; cada sinal pode acionar no máximo um trade graças às guardas `_lastLongEntryTime` / `_lastShortEntryTime`.
- Todas as ações são executadas com ordens de mercado. As posições opostas existentes são fechadas antes das reversões.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `CandleType` | Stream de dados usado para análise. Suporta qualquer `DataType` (candles de tempo, tick, volume). | Período H4 |
| `Volume` | Tamanho de ordem fixo que substitui os blocos de gestão monetária do EA. Deve ser > 0. | 0.1 |
| `FirstMethod`, `FirstLength`, `FirstPhase` | Primeiro estágio de suavização para momentum e momentum absoluto. | SMA, 12, 15 |
| `SecondMethod`, `SecondLength`, `SecondPhase` | Segundo estágio de suavização. | SMA, 12, 15 |
| `PriceMode` | Opção de preço aplicado que alimenta o oscilador. | Close |
| `SignalBar` | Deslocamento de barra usado para avaliar sinais (1 = última barra fechada). | 1 |
| `TriggerShift` | Atraso aplicado à linha de trigger (1 reproduz o indicador original). | 1 |
| `EnableLongEntries` / `EnableShortEntries` | Permite abrir trades comprados/vendidos. | true |
| `EnableLongExits` / `EnableShortExits` | Permite fechar posições em contexto oposto. | true |
| `StopLossPoints` | Distância de stop-loss em pontos de preço (convertida com o `PriceStep` do instrumento). | 1000 |
| `TakeProfitPoints` | Distância de take-profit em pontos de preço. | 2000 |

## Gestão de risco
- O EA original dependia de funções auxiliares de `TradeAlgorithms.mqh` para colocação de SL/TP. A versão C# chama `StartProtection` com as distâncias selecionadas convertidas para `UnitTypes.Point`.
- Se qualquer distância for definida como 0, a ordem de proteção correspondente é omitida.
- Não são implementados trailing stops ou escalonamento de posição; eles correspondem ao comportamento do MetaTrader para este consultor.

## Diferenças da versão MetaTrader
- O sizing de lote baseado em margem (`MM` e `MMMode`) é substituído por um parâmetro `Volume` fixo. Isso mantém o comportamento determinístico entre corretores e evita replicar a lógica de alavancagem específica de cada conta.
- O deslizamento (`Deviation_`) não é emulado porque as ordens de mercado do StockSharp não expõem um parâmetro de deslizamento.
- A suavização do indicador é totalmente reconstruída usando indicadores do StockSharp (incluindo o tratamento de fase Jurik via reflexão), portanto os valores de sinal são consistentes com os buffers originais.
- A implementação Python é intencionalmente omitida conforme solicitado.

## Notas de uso
- Garantir que o instrumento selecionado forneça o tipo de candle solicitado por `CandleType`. Para períodos padrão usar `TimeSpan.FromHours(x).TimeFrame()`.
- `SignalBar` deve ser ≥ `TriggerShift` para obter valores de trigger válidos; caso contrário, os sinais são ignorados até que histórico suficiente seja acumulado.
- Como a estratégia reage em candles terminados, habilitar o registro de ordens em tempo real apenas após `IsFormedAndOnlineAndAllowTrading()` se tornar true.
- A área do gráfico visualiza candles de preço e trades executados; os indicadores são reconstruídos internamente e não são traçados automaticamente.
- Para reproduzir os padrões do MetaTrader: manter todas as configurações de suavização em SMA com comprimento 12, manter ambos os toggles de entrada e saída habilitados, e usar as distâncias de stop/take padrão.
