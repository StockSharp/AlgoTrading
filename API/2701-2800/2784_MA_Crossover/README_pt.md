# Estratégia de Cruzamento de MA em Múltiplos Períodos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz a ideia do expert advisor original **MA Crossover** para MetaTrader 4. Ela compara duas médias móveis que podem vir de diferentes períodos. Um cruzamento altista (MA rápida acima da MA lenta) abre uma posição comprada, enquanto um cruzamento baixista abre uma posição vendida. Filtros opcionais controlam a direção de operação permitida, o horário de trading ativo e um guardião de equity. A lógica interna de stop-loss, take-profit e trailing emula as saídas "ocultas" da versão MQL.

## Lógica de trading

1. Inscrever-se em dois fluxos de velas (períodos atual e anterior) e calcular o tipo selecionado de médias móveis.
2. Aplicar os deslocamentos de barra configurados nos valores da média móvel antes de compará-los.
3. Ignorar velas não concluídas e aguardar ambas as médias móveis estarem formadas.
4. Pular o trading fora da janela de dia/hora configurada ou quando o guardião de equity for acionado.
5. Em um cruzamento altista:
   - Opcionalmente fechar uma posição vendida se `ClosePositionsOnCross = true`.
   - Abrir uma posição comprada se o trading comprado for permitido.
6. Em um cruzamento baixista:
   - Opcionalmente fechar uma posição comprada se `ClosePositionsOnCross = true`.
   - Abrir uma posição vendida se o trading vendido for permitido.
7. Gerenciar a posição aberta com regras de stop-loss, take-profit e trailing expressas como percentuais do preço de entrada.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `AllowedDirection` | Filtro de direção de operação (`LongOnly`, `ShortOnly`, `LongAndShort`). |
| `ClosePositionsOnCross` | Fechar a posição oposta quando um cruzamento aparecer antes de abrir uma nova operação. |
| `MaType` | Tipo de cálculo de média móvel (`Simple`, `Exponential`, `Smoothed`, `Weighted`). |
| `CurrentMaPeriod` | Período para a média móvel rápida. |
| `PreviousPeriodAddition` | Comprimento extra adicionado à média móvel lenta (`PreviousMaPeriod = CurrentMaPeriod + addition`). |
| `CurrentShift` / `PreviousShift` | Número de barras concluídas usadas para deslocar os valores da média móvel para trás. |
| `CurrentCandleType` / `PreviousCandleType` | Dados de vela para as médias móveis rápidas e lentas. |
| `StopLossPercent` | Distância de stop-loss em percentual do preço de entrada (saída oculta). |
| `TrailingStopPercent` | Distância de trailing stop em percentual baseado no melhor preço alcançado. |
| `TakeProfitPercent` | Distância de take-profit em percentual do preço de entrada (saída oculta). |
| `StartDay` / `EndDay` | Filtro de dia da semana para atividade de trading. |
| `StartTime` / `EndTime` | Janela de tempo intradiário para abertura de novas operações. |
| `ClosePositionsOnMinEquity` | Fechar todas as posições quando o guardião de equity for acionado. |
| `MinimumEquityPercent` | Percentual mínimo do valor inicial do portfólio permitido pelo guardião de equity. |

## Gestão de risco

- A estratégia calcula os níveis de stop-loss, take-profit e trailing internamente e sai via ordens de mercado, imitando a lógica de proteção oculta do script MQL.
- `MinimumEquityPercent` armazena o valor inicial do portfólio na inicialização e pode acionar um nivelamento forçado se o equity cair abaixo do limite.
- O tamanho da posição é controlado através da propriedade base `Strategy.Volume`. O volume padrão é definido como `1`.

## Notas de uso

- A estratégia requer dados de velas para ambos os períodos configurados. Certifique-se de que os conectores associados suportem os períodos solicitados.
- Quando ambas as médias móveis usam o mesmo período, a estratégia ainda se inscreve em dois fluxos para manter a lógica simétrica.
- Como as saídas por stop e take-profit são executadas via ordens de mercado, nenhuma ordem de proteção permanece no livro de ordens.
- Os parâmetros correspondem às entradas principais do expert advisor MQL original, enquanto os recursos de gestão de risco/margem que dependem de funções específicas do broker (hedge, averaging) são intencionalmente omitidos.

## Diferenças da versão MQL

- Recursos de averaging (`Average_Up`, `Average_Down`) e configurações de hedge não estão implementados para manter a lógica compatível com a API de alto nível do StockSharp.
- O guardião de equity usa o valor do portfólio do StockSharp em vez de cálculos específicos de margem livre.
- As saídas por risco são executadas através de ordens de mercado em eventos de fechamento de vela e são, portanto, sempre ocultas do livro de ordens.
