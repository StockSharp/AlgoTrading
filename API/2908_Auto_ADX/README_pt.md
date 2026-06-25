# Estratégia Auto ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Auto ADX** é um port direto do consultor especialista MetaTrader `Auto ADX.mq5` para a API de alto nível do StockSharp. A estratégia avalia a força do Índice Direcional Médio (ADX) e a relação entre os componentes +DI e -DI para determinar a direção do trade. Reproduz os controles de risco originais, incluindo stop-loss, take-profit, sinais reversíveis e trailing stops baseados em pips, enquanto adota conceitos do StockSharp como assinaturas de candles e vínculos de indicadores.

## Lógica de Trading
- **Fonte de Candles** – A estratégia assina um tipo de candle configurável (padrão: período de 1 hora) e processa apenas candles concluídos para evitar ruído intrabar.
- **Cálculo ADX** – Um único indicador `AverageDirectionalIndex` é vinculado através de `BindEx`, dando acesso ao valor ADX suavizado bem como às linhas +DI e -DI.
- **Entrada Comprada** – Acionada quando:
  - +DI é maior que -DI (momentum direcional positivo),
  - ADX está acima do nível ADX configurável, e
  - ADX está subindo comparado com o candle anterior.
- **Entrada Vendida** – Acionada quando:
  - -DI é maior que +DI (momentum direcional negativo),
  - ADX está abaixo do nível configurado, e
  - ADX está caindo versus o candle anterior.
- **Modo Reverso** – Quando `ReverseSignals` está habilitado (comportamento padrão), posições abertas são fechadas se:
  - Uma posição comprada vê +DI cair abaixo de -DI **ou** ADX decresce,
  - Uma posição vendida vê +DI subir acima de -DI **ou** ADX sobe.
- **Dimensionamento de Posição** – As ordens são emitidas com o `Volume` da estratégia. O tratamento de reversão depende de `ClosePosition()` para sair de toda a exposição antes que um novo sinal seja considerado.

## Gestão de Risco
- **Stop-Loss / Take-Profit** – Convertidos de entradas em pips para distâncias de preço absolutas usando o `PriceStep` do instrumento. O auxiliar `StartProtection` do StockSharp coloca as ordens protetoras com execução de mercado opcional.
- **Trailing Stop** – A lógica original de trailing baseada em pips é replicada:
  - O trailing é ativado apenas depois que o lucro não realizado excede a distância de trailing.
  - O nível de stop se move em passos de tamanho pip (`TrailingStepPips`).
  - Uma posição comprada sai se o preço imprimir abaixo do trailing stop; uma vendida sai quando o preço sobe acima do trailing stop.
- **Conversão de Pip** – Para imitar a implementação MQL, o tamanho do pip é igual ao `PriceStep`, multiplicado por 10 quando o instrumento usa preços de 3 ou 5 decimais. Isso mantém o comportamento consistente entre símbolos forex.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `StopLossPips` | 50 | Distância do stop protetor em pips. Definir como zero para desabilitar o stop-loss. |
| `TakeProfitPips` | 50 | Distância do alvo de lucro em pips. Definir como zero para desabilitar o take-profit. |
| `TrailingStopPips` | 5 | Tamanho do trailing stop em pips. Definir como zero para desabilitar o trailing. |
| `TrailingStepPips` | 5 | Ganho incremental mínimo (em pips) antes de deslocar o trailing stop. Deve ser positivo quando o trailing está habilitado. |
| `AdxPeriod` | 14 | Período de média para o indicador ADX. |
| `AdxLevel` | 30 | Limiar de força ADX que filtra as entradas. |
| `ReverseSignals` | true | Habilita o fechamento de posições existentes quando a relação DI ou a inclinação ADX muda. |
| `CandleType` | 1 hora | Tipo de candle usado para análise e trading. |

## Notas de Implementação
- `BindEx` é usado para acessar o `AverageDirectionalIndexValue` completo, garantindo que nunca dependemos da recuperação manual de valores de indicadores.
- A lógica de trailing mantém registro do último nível de stop e o move apenas quando o preço progride pelo menos `TrailingStepPips` a favor da posição, replicando o comportamento de passo de trailing MQL.
- Todos os comentários inline no código-fonte C# estão em inglês para satisfazer as diretrizes do repositório.
- A estratégia é autônoma dentro de `API/2908_Auto_ADX/CS/AutoAdxStrategy.cs`; não há contraparte Python conforme os requisitos.

## Dicas de Uso
1. Anexar a estratégia a um instrumento com metadados `PriceStep` corretos para que a conversão de pips permaneça precisa.
2. Ajustar `AdxLevel` para corresponder ao perfil de volatilidade do instrumento negociado — limiares mais altos reduzem a frequência de sinais.
3. Quando o trailing está desabilitado (`TrailingStopPips = 0`), `TrailingStepPips` é ignorado, reproduzindo o comportamento do consultor especialista original.
4. Fazer backtests em múltiplos mercados para validar as distâncias de proteção baseadas em pips e confirmar que a filtragem de inclinação ADX corresponde às expectativas.
