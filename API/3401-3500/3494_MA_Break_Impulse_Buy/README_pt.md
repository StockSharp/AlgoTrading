# Estratégia de compra por impulso MA Break
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o consultor especialista "MA break mt4 buy" usando o API de alto nível de StockSharp. Ele se concentra na identificação de fortes rompimentos de alta após uma consolidação silenciosa. A lógica de entrada procura uma sequência de filtros de média móvel exponencial (EMA), uma fase de mercado tranquila e, em seguida, uma poderosa vela de impulso de alta que interage com um rompimento EMA. A estratégia abre apenas posições **longas**.

## Lógica de negociação
1. **EMA Filtros de tendências**
   - Dois EMA pares são avaliados na vela concluída anterior (`shift = 1`).
   - `EMA(FirstFastPeriod)` deve ser maior que `EMA(FirstSlowPeriod)`.
   - `EMA(SecondFastPeriod)` deve ser maior que `EMA(SecondSlowPeriod)`.
2. **Seleção de velas de impulso**
   - A vela de impulso é a última barra concluída (shift 1).
   - Seu preço de abertura deve estar acima de `TrendMaPeriod` EMA.
   - Seu mínimo deve tocar ou cair abaixo de `BreakoutMaPeriod` EMA.
   - A vela deve ser de alta (`Close > Open`).
   - O intervalo da vela deve estar entre `CandleMinSize` e `CandleMaxSize` (convertido de pips usando `Security.PriceStep`).
   - O pavio superior não deve exceder `UpperWickLimit` por cento do intervalo da vela. O pavio inferior deve ter pelo menos `LowerWickFloor` por cento do intervalo.
3. **Barras silenciosas e força de impulso**
   - A estratégia varre `QuietBarsCount` velas anteriores à vela de impulso (deslocamentos ≥ 2) e registra a faixa máxima-baixa.
   - Esta faixa silenciosa deve ser maior que `QuietBarsMinRange` (pips → preço).
   - O corpo da vela de impulso (`Close - Open`) deve ser pelo menos `ImpulseStrength × quietRange`.
4. **Gerenciamento de posição**
   - Uma ordem de compra a mercado é enviada quando todas as condições são atendidas e nenhuma posição está aberta no momento.
   - As ordens protetoras de stop-loss e take-profit são gerenciadas por meio de `StartProtection`, usando entradas de pip convertidas por meio de `Security.PriceStep`.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `FirstFastPeriod` | 20 | EMA rápida usado no primeiro filtro de tendência. |
| `FirstSlowPeriod` | 30 | EMA lenta usado no primeiro filtro de tendência. |
| `SecondFastPeriod` | 30 | EMA rápida para o segundo filtro de tendência. |
| `SecondSlowPeriod` | 50 | EMA lenta para o segundo filtro de tendência. |
| `TrendMaPeriod` | 30 | EMA que a vela de impulso aberta deve exceder. |
| `BreakoutMaPeriod` | 20 | EMA que a vela de impulso deve tocar. |
| `QuietBarsCount` | 2 | Número de velas calmas antes da avaliação do impulso. |
| `QuietBarsMinRange` | 0,0 | Faixa mínima de silêncio (pips). |
| `ImpulseStrength` | 1.1 | Multiplicador aplicado à faixa silenciosa para validar o tamanho do corpo do impulso. |
| `UpperWickLimit` | 100,0 | Pavio superior máximo como porcentagem do intervalo da vela. |
| `LowerWickFloor` | 0,0 | Pavio inferior mínimo como porcentagem do intervalo da vela. |
| `CandleMinSize` | 0,0 | Faixa mínima permitida da vela de impulso em pips. |
| `CandleMaxSize` | 100,0 | Faixa máxima permitida da vela de impulso em pips. |
| `VolumeSize` | 0,01 | Volume de negociação enviado com `BuyMarket`. Normalizado para troca `VolumeStep`. |
| `StopLossPips` | 20,0 | Distância de stop-loss em pips (convertida com `PriceStep`). |
| `TakeProfitPips` | 20,0 | Distância de lucro em pips (convertida com `PriceStep`). |
| `CandleType` | Período de 15 minutos | Tipo de dados Candle solicitado do conector. |

## Notas de implementação
- A estratégia usa assinaturas StockSharp de alto nível `Bind` para manter os cálculos dos indicadores sincronizados com as atualizações das velas.
- Todos os cálculos dependem apenas de velas finalizadas (`CandleStates.Finished`).
- Os filtros de faixa silenciosa e de tamanho de vela convertem internamente os valores de pip em unidades de preço usando `Security.PriceStep`. Se o instrumento não reportar `PriceStep`, um substituto de `1` será usado, correspondendo à lógica MQL de multiplicação pelo valor pip.
- `StartProtection` é ativado uma vez durante `OnStarted` para que cada nova posição receba o stop-loss e o take-profit configurados.
- O buffer do histórico de velas mantém apenas as últimas `QuietBarsCount + 3` entradas para avaliar o período de silêncio e a vela de impulso com eficiência.

## Dicas de uso
- Certifique-se de que o instrumento conectado forneça `PriceStep`, `VolumeStep` e limites de volume para que as conversões de pip e volume permaneçam precisas.
- Ajuste EMA períodos e parâmetros de impulso à volatilidade do instrumento. Um `ImpulseStrength` mais baixo reagirá a rompimentos menores, enquanto um valor mais alto filtra apenas os movimentos mais fortes.
- A estratégia é projetada para uma posição aberta por vez. Posições externas sobre o mesmo título podem impedir novas entradas.
