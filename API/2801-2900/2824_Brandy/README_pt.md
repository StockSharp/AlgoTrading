# Estratégia Brandy (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A estratégia Brandy é uma portagem direta do Expert Advisor do MetaTrader 5 *Brandy (edição de barabashkakvn)*. Combina duas médias móveis configuráveis e avalia suas posições relativas em velas fechadas para decidir se abre uma posição comprada ou vendida. A lógica original também impõe controles opcionais de stop loss, take profit e trailing stop expressos em pips. Esta versão em C# reproduz fielmente esses comportamentos sobre a API de estratégia de alto nível do StockSharp.

A estratégia calcula uma média móvel "rápida" no fluxo de preços de abertura e uma média móvel "lenta" no fluxo de preços de fechamento. Ambos os indicadores têm parâmetros independentes de período, método de suavização, fonte de preço, referência de barra de sinal e deslocamento. Os sinais são gerados quando os valores de MA da barra anterior estão no mesmo lado dos valores de sinal respectivos. A lógica protetora verifica a média móvel baseada na abertura a cada vela e sai imediatamente da operação se a condição de tendência não for mais satisfeita. O gerenciamento de risco adicional é implementado com distâncias opcionais de stop loss, take profit e trailing stop, todas medidas em pips e convertidas a preços absolutos usando o tamanho do tick do instrumento com um ajuste de pip de cinco dígitos.

## Lógica de Trading
1. Em cada vela terminada, a estratégia atualiza as médias móveis de preço de abertura e fechamento usando o método de suavização configurado e o preço aplicado. Os valores históricos de MA são armazenados em buffer para que o código possa emular o comportamento de deslocamento de `iMA` do Expert Advisor original.
2. Quando não há posição ativa, uma operação comprada é aberta se:
   - O valor de MA baseado em abertura da barra anterior é maior que o valor de sinal configurado (possivelmente deslocado);
   - O valor de MA baseado em fechamento da barra anterior também é maior que sua referência de sinal (note que o EA original compara contra o indicador baseado em abertura para esta verificação, e a portagem mantém essa peculiaridade por compatibilidade).
3. Uma operação vendida é aberta quando ambas as médias móveis estão abaixo de suas referências de sinal respectivas.
4. Enquanto há uma posição ativa, a estratégia avalia saídas em cada vela terminada na seguinte ordem:
   - Reversão de tendência: se a MA baseada em abertura cai abaixo do valor de sinal (para compradas) ou sobe acima (para vendidas), a posição é fechada imediatamente a mercado.
   - Atualização do trailing stop: quando habilitado e o movimento a favor da operação excede *trailing stop + trailing step* (convertido a preços absolutos), o nível de stop é ajustado para manter uma distância de *trailing stop* do último fechamento.
   - Take profit: se o range da vela toca o objetivo de lucro, a operação é encerrada a mercado.
   - Stop loss: se o range da vela viola o nível de stop protetor, a operação é fechada.
5. Todo o volume é fixo e determinado pelo parâmetro `TradeVolume`. O valor padrão replica a configuração de 0,1 lotes da versão MT5.

## Referência de Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradeVolume` | Tamanho da ordem de mercado em lotes.
| `StopLossPips` | Distância do stop protetor, medida em pips (0 desabilita).
| `TakeProfitPips` | Distância do objetivo de lucro em pips (0 desabilita).
| `TrailingStopPips` | Distância do trailing stop em pips. Requer que `TrailingStepPips` seja positivo.
| `TrailingStepPips` | Movimento adicional de pips necessário antes de avançar o trailing stop. Deve ser diferente de zero quando o trailing stop está ativo.
| `MaClosePeriod`, `MaOpenPeriod` | Comprimentos de média móvel para as séries de fechamento e abertura respectivamente.
| `MaCloseShift`, `MaOpenShift` | Deslocamentos aplicados aos buffers de MA (número de barras).
| `MaCloseSignalBar`, `MaOpenSignalBar` | Índices de barras usados como referências de comparação. Zero corresponde ao valor mais recente, um se refere à barra anterior, e assim por diante.
| `MaCloseMethod`, `MaOpenMethod` | Métodos de suavização de média móvel (SMA, EMA, SMMA, LWMA).
| `MaCloseAppliedPrice`, `MaOpenAppliedPrice` | Fonte de preço de vela para cada indicador (fechamento, abertura, máxima, mínima, mediana, típico, ponderado).
| `CandleType` | Período das velas solicitadas da fonte de dados.

## Notas de Implementação
- O tamanho do pip é calculado de `Security.PriceStep` e multiplicado por 10 quando o instrumento expõe 3 ou 5 casas decimais, refletindo o ajuste do MetaTrader entre pontos e pips.
- O histórico do indicador é retido usando filas delimitadas para que a estratégia possa reproduzir chamadas `iMA` com índices de barra de sinal arbitrários e deslocamentos positivos sem depender de acessores de indicadores proibidos.
- A condição de fechamento para a média móvel baseada em fechamento compara intencionalmente contra o buffer de MA de **abertura** porque o código-fonte original invocava `iMAGet(handle_iMAOpen, MaClose_SignalBar)`. Esta portagem mantém o comportamento para preservar a compatibilidade com configurações legadas.
- Stops e lógica de trailing são executados em velas terminadas e aproximam as modificações de ordens realizadas pelo Expert Advisor respeitando a API de alto nível do StockSharp.

## Dicas de Uso
- Configure o parâmetro `CandleType` para corresponder ao período usado pelo EA original (tipicamente um único período de instrumento).
- Mantenha `TrailingStopPips` em zero se não for desejado comportamento de trailing; caso contrário, garanta que `TrailingStepPips` seja estritamente positivo para evitar o erro de inicialização imposto pela estratégia.
- Ao fazer backtesting no StockSharp, certifique-se de que `PriceStep` e `Decimals` do instrumento reflitam a definição de pip pretendida para que as distâncias de risco sejam convertidas corretamente.
