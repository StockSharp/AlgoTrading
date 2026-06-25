# Estratégia de Trade Separado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A Estratégia de Trade Separado é uma conversão do consultor especialista MetaTrader 5 "Separate trade". Preserva a lógica multi-filtro original enquanto adota a API de alto nível do StockSharp para gerenciamento robusto de ordens e tratamento de indicadores. A estratégia tenta capturar viradas quietas do mercado quando a volatilidade e a dispersão estão suprimidas. Apenas uma posição líquida é mantida de cada vez, o que reflete a intenção do código original que limitava o número de posições simultâneas.

## Indicadores e Dados
- Duas médias móvies com método configurável (SMA, EMA, SMMA ou LWMA) e fonte de preço compartilhada.
- Average True Range (ATR) com períodos e limites separados para filtros comprados e vendidos.
- Desvio padrão usando o mesmo preço aplicado que as médias móvies, novamente com períodos e limites específicos de direção.
- As velas são fornecidas através de um parâmetro `DataType` configurável para que a estratégia possa ser anexada a qualquer período ou construtor de velas personalizado.

## Parâmetros
| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Tamanho da ordem expresso em lotes. | `1` |
| `SlowMaPeriod` | Período da média móvel mais lenta. | `65` |
| `FastMaPeriod` | Período da média móvel mais rápida. | `14` |
| `MaMethod` | Método de suavização aplicado a ambas as médias móvies (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). | `Exponential` |
| `PriceType` | Entrada de preço para as médias móvies e desvio padrão (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). | `Close` |
| `StopLossBuyPips` / `StopLossSellPips` | Distância do stop-loss para trades comprados e vendidos em pips (0 desabilita o stop). | `50` |
| `TakeProfitBuyPips` / `TakeProfitSellPips` | Distância do take-profit para trades comprados e vendidos em pips (0 desabilita o take-profit). | `50` |
| `TrailingStopPips` | Distância do trailing stop em pips. | `5` |
| `TrailingStepPips` | Avanço mínimo de lucro em pips antes que o trailing stop seja movido. Deve ser positivo quando o trailing estiver habilitado. | `5` |
| `MaxPositions` | Máximo de posições líquidas simultâneas permitidas. A versão StockSharp opera com uma única posição agregada mesmo quando o valor é maior que um. | `1` |
| `DeltaBuyPips` / `DeltaSellPips` | Distância máxima permitida entre as médias rápida e lenta (por direção). Um valor de zero desabilita o filtro de distância. | `2` |
| `AtrPeriodBuy` / `AtrPeriodSell` | Período de retrospectiva do ATR para os filtros comprado e vendido. | `26` |
| `AtrLevelBuy` / `AtrLevelSell` | Limite superior do ATR que não deve ser excedido antes de entrar em um trade. | `0.0016` |
| `StdDevPeriodBuy` / `StdDevPeriodSell` | Período de retrospectiva do desvio padrão para os filtros comprado e vendido. | `54` |
| `StdDevLevelBuy` / `StdDevLevelSell` | Limite do desvio padrão que não deve ser excedido antes de entrar em um trade. | `0.0051` |
| `CandleType` | Tipo de dados de velas usado pela assinatura. | `TimeSpan.FromMinutes(15)` |

## Lógica de Trading
1. **Sincronização de barras** – a estratégia age apenas em velas terminadas recebidas da assinatura configurada. Isso replica o guarda de nova barra `OnTick` do script MetaTrader.
2. **Filtros de indicadores** – para entradas compradas a MA lenta deve estar abaixo da MA rápida, ATR deve estar abaixo de `AtrLevelBuy`, desvio padrão deve estar abaixo de `StdDevLevelBuy`, e a distância de MA deve ser menor que `DeltaBuyPips` (se o delta for positivo). As entradas vendidas invertem as condições e usam seus próprios parâmetros de ATR e desvio.
3. **Controle de posição** – trades são apenas realizados quando não há posição aberta e o último tempo de entrada para o lado respectivo é mais antigo que a vela atual. Isso previne re-entradas dentro da mesma barra, correspondendo à verificação `m_last_deal_IN_*` no EA fonte.
4. **Execução de ordens** – ordens de mercado são colocadas com o volume configurado. Trades de reversão aplanam automaticamente a posição atual antes de abrir uma nova graças à quantidade `Volume + Math.Abs(Position)` que corresponde ao comportamento MQL de fechar a exposição oposta.

## Gestão de Risco
- **Conversão de pips** – distâncias em pips são convertidas usando o `PriceStep` do instrumento. Para instrumentos cotados com 3 ou 5 decimais, o tamanho do pip equivale a `PriceStep * 10`, refletindo a lógica original `digits_adjust`.
- **Stop-loss / take-profit** – a estratégia rastreia níveis de preço internamente e sai quando o intervalo da vela toca o stop ou o alvo especificado. Ambos podem ser desabilitados definindo a distância em pips como zero.
- **Trailing stop** – uma vez que o preço avança além de `TrailingStopPips + TrailingStepPips`, o stop é movido para manter a distância de trailing. O requisito do passo de trailing corresponde à implementação do MetaTrader e evita mover o stop por uma quantidade insignificante.

## Notas de Implementação
- A estratégia usa uma única posição agregada porque o StockSharp trabalha com posições líquidas por padrão. Embora o parâmetro `MaxPositions` seja mantido por compatibilidade, exceder um simplesmente previne novas entradas até que a posição atual seja fechada.
- Os valores do indicador são calculados usando as classes de indicadores do StockSharp e a infraestrutura `Bind` para evitar acesso manual ao buffer conforme exigido pelas diretrizes do projeto.
- A conversão mantém todos os comentários em inglês e mapeia cada entrada original a um `StrategyParam` dedicado para que a otimização e a integração do Designer permaneçam disponíveis.
- Quando `TrailingStopPips` é positivo, `TrailingStepPips` também deve ser positivo. O código para a estratégia cedo e escreve uma mensagem de erro se este requisito for violado, reproduzindo a verificação de segurança do especialista MQL.
