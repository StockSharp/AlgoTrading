# Estratégia de JK Synchro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de JK Synchro** é um port StockSharp do consultor especialista MetaTrader 5 "JK synchro" (MQL ID 2415). O robô original conta quantas das velas mais recentes fecharam abaixo ou acima de suas aberturas e então abre uma posição na direção dominante. Este port replica o comportamento enquanto adiciona parâmetros fortemente tipados, hooks de gerenciamento de risco integrados e registro detalhado via StockSharp.

## Lógica de trading

1. Subscrever à fonte de velas definida por `CandleType` e aguardar velas concluídas.
2. Manter uma janela deslizante de `AnalysisPeriod` velas. Para cada vela:
   - Incrementar o contador **baixista** quando `Open > Close`.
   - Incrementar o contador **altista** quando `Open < Close`.
   - Ignorar velas doji onde `Open == Close`.
3. Quando a janela estiver preenchida, verificar a dominância:
   - Se as velas baixistas superam as altistas, preparar uma entrada **comprada**.
   - Se as velas altistas superam as baixistas, preparar uma entrada **vendida**.
4. Antes de entrar em uma operação, a estratégia verifica:
   - A estratégia está online e tem permissão para negociar (`IsFormedAndOnlineAndAllowTrading`).
   - A hora atual está entre `StartHour` e `EndHour` (inclusive).
   - O resfriamento definido por `PauseBetweenTradesSeconds` decorreu desde a última entrada.
   - Adicionar outro lote manteria a exposição líquida dentro de `MaxPositions * OrderVolume`.
5. Quando um sinal aparece enquanto se mantém uma posição oposta, a estratégia primeiro fecha essa posição e aguarda a próxima vela antes de potencialmente entrar na nova direção.
6. Os níveis protetores de stop-loss, take-profit e trailing stop são expressos em pips e automaticamente traduzidos em offsets de preço com base no tamanho do tick do instrumento.

## Gerenciamento de risco

- **Stop Loss / Take Profit**: Níveis opcionais definidos em pips. São recalculados a cada mudança de posição e verificados a cada vela concluída.
- **Trailing Stop**: Ativado quando tanto `TrailingStopPips` quanto `TrailingStepPips` são positivos. Uma vez que a operação se move a favor pelo menos `TrailingStop + TrailingStep`, o stop acompanha o preço usando o passo configurado.
- **Limite de posição**: A posição líquida absoluta não pode exceder `MaxPositions * OrderVolume`.
- **Pausa de entrada**: A estratégia registra o timestamp de cada execução e aplica uma pausa antes de abrir outra operação.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `OrderVolume` | 0.1 | Volume colocado com cada ordem de mercado. |
| `MaxPositions` | 10 | Número máximo de lotes permitidos por direção. |
| `AnalysisPeriod` | 18 | Número de velas concluídas consideradas ao contar movimentos altistas versus baixistas. |
| `PauseBetweenTradesSeconds` | 540 | Resfriamento em segundos após qualquer entrada antes de uma nova poder ser aberta. |
| `StartHour` | 3 | Hora de início (inclusive) da janela de trading, hora do servidor. |
| `EndHour` | 6 | Hora de fim (inclusive) da janela de trading, hora do servidor. |
| `StopLossPips` | 50 | Distância de stop-loss expressa em pips. Definir como 0 para desabilitar. |
| `TakeProfitPips` | 150 | Distância de take-profit em pips. Definir como 0 para desabilitar. |
| `TrailingStopPips` | 15 | Distância do trailing stop em pips. Definir como 0 para desabilitar o trailing. |
| `TrailingStepPips` | 5 | Distância adicional em pips antes de o trailing stop ser atualizado. Deve ser positivo quando o trailing está habilitado. |
| `CandleType` | Período de 15 minutos | Fonte de velas usada para todos os cálculos. |

## Notas de implementação

- A API de alto nível do StockSharp é usada em todo momento (`SubscribeCandles`, `.Bind`, `BuyMarket`, `SellMarket`).
- Os timestamps de entrada são capturados dentro de `OnPositionChanged` para implementar a lógica de pausa exatamente como o EA original, que aguardava um tempo fixo após cada entrada.
- O tamanho do pip é derivado de `Security.PriceStep` e `Security.Decimals`; para instrumentos de 3 ou 5 dígitos o multiplicador é ajustado automaticamente.
- As saídas são tratadas em velas fechadas comparando o máximo/mínimo com os níveis calculados de stop e alvo.
- Os trailing stops imitam a lógica do MetaTrader: começam a mover somente após o lucro exceder `TrailingStop + TrailingStep` e nunca se revertem.

## Dicas de uso

1. Alinhar `OrderVolume` e `MaxPositions` com o tamanho do contrato do seu broker para manter a exposição sob controle.
2. Escolher `AnalysisPeriod` de acordo com o período das velas. Períodos mais curtos geralmente requerem janelas maiores para evitar ruído.
3. Ajustar a janela de trading para coincidir com as horas ativas do instrumento (ex.: sessão europeia para pares baseados em EUR).
4. Fazer backtest de diferentes combinações de stop, alvo e configurações de trailing — o EA original frequentemente operava com alvos fixos ou trailing stops dependendo das condições de mercado.

## Diferenças em relação à versão MQL

- O port StockSharp usa um modelo de exposição líquida. Ao trocar de direção, a posição existente é fechada primeiro, enquanto a versão MetaTrader podia manter posições hedgeadas.
- O registro e gerenciamento de parâmetros aproveitam as facilidades do StockSharp, tornando a otimização e integração com a interface mais fáceis.
- O trailing stop é avaliado em velas concluídas, o que é consistente com outros ports de estratégias StockSharp e evita reagir a barras incompletas.

Com essas considerações, a estratégia JK Synchro pode ser negociada, analisada e otimizada diretamente dentro do ecossistema StockSharp.
