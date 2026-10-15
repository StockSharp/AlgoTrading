# Estratégia BARS Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia BARS Alligator é um port direto do consultor especialista do MetaTrader com o mesmo nome. Ela depende do indicador Alligator de Bill Williams para detectar tendências emergentes: quando a linha verde dos lábios (lips) cruza acima da linha azul da mandíbula (jaw), o sistema trata isso como um rompimento altista, enquanto um cruzamento para baixo sinaliza impulso baixista. As saídas dependem do lips cruzando a linha vermelha dos dentes (teeth) para que as posições sejam fechadas assim que o impulso desaparece. As distâncias de stop-loss de proteção, take-profit e trailing stop são configuradas em pips e automaticamente convertidas em unidades de preço com base no passo de preço do instrumento e na precisão decimal.

## Lógica de trading

1. **Construção do indicador**
   - Três médias móveis com comprimentos, deslocamentos e tipo configuráveis (simples, exponencial, suavizada ou ponderada) formam o Alligator.
   - O preço aplicado pode ser o fechamento, abertura, máxima, mínima, mediano, típico ou preço ponderado de cada vela.
   - Os deslocamentos são respeitados armazenando um pequeno buffer rotativo para cada linha para que os cruzamentos usem os mesmos valores que apareceriam em um gráfico do MetaTrader.
2. **Condições de entrada**
   - **Comprado**: a linha lips na barra anterior está acima da jaw e estava abaixo duas barras atrás (cruzamento altista para cima).
   - **Vendido**: a linha lips na barra anterior está abaixo da jaw e estava acima duas barras atrás (cruzamento baixista para baixo).
   - Novas entradas são permitidas apenas se a posição atual está plana ou já alinhada com a direção do sinal e o tamanho agregado da posição permanece abaixo de `MaxPositions × OrderVolume` (ou o equivalente dimensionado por risco).
3. **Condições de saída**
   - **Saída comprada**: a linha lips cruza abaixo da linha teeth e a posição é lucrativa em relação ao preço médio de entrada.
   - **Saída vendida**: a linha lips cruza acima da linha teeth e a posição é lucrativa.
   - As saídas também ocorrem quando os níveis estáticos de stop-loss ou take-profit são violados.
4. **Trailing stop**
   - Quando habilitado, um trailing stop reposiciona o stop de proteção assim que o preço se move além de `TrailingStopPips + TrailingStepPips` na direção do trade. O stop então segue o preço a uma distância de `TrailingStopPips` pips, mas apenas avança se o preço fizer novo progresso de pelo menos `TrailingStepPips` pips.
5. **Gestão monetária**
   - Com `MoneyMode = FixedVolume`, as ordens usam o tamanho de `OrderVolume` diretamente.
   - Com `MoneyMode = RiskPercent`, a estratégia aloca volume de forma que o percentual configurado `MoneyValue` do capital do portfólio seria perdido se o stop-loss fosse atingido. O risco por unidade equivale à distância do stop-loss expressa em unidades de preço. O resultado é arredondado para baixo até o `VolumeStep` mais próximo (ou para 1 quando informações de step estão faltando).

## Parâmetros

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `CandleType` | `DataType` | `TimeSpan.FromHours(1).TimeFrame()` | Período usado para cálculos do Alligator. |
| `OrderVolume` | `decimal` | `0.1` | Volume de trade fixo quando `MoneyMode` é `FixedVolume`. |
| `MoneyMode` | `MoneyManagementMode` | `FixedVolume` | Escolhe entre volume fixo e dimensionamento por percentual de risco. |
| `MoneyValue` | `decimal` | `1` | Percentual de risco aplicado quando `MoneyMode` é `RiskPercent`; ignorado caso contrário. |
| `MaxPositions` | `int` | `1` | Número máximo de entradas aditivas por direção (expresso como múltiplos do volume de ordem calculado). |
| `StopLossPips` | `int` | `150` | Distância de stop-loss em pips. Zero desabilita o stop de proteção. |
| `TakeProfitPips` | `int` | `150` | Distância de take-profit em pips. Zero desabilita o alvo de lucro. |
| `TrailingStopPips` | `int` | `5` | Distância do trailing stop em pips. Zero desabilita o trailing. |
| `TrailingStepPips` | `int` | `5` | Distância extra que o preço deve percorrer antes de o trailing stop avançar. Deve ser positivo quando o trailing está habilitado. |
| `JawPeriod` | `int` | `13` | Comprimento da média móvil jaw. |
| `JawShift` | `int` | `8` | Deslocamento para frente (em barras) aplicado à série jaw. |
| `TeethPeriod` | `int` | `8` | Comprimento da média móvil teeth. |
| `TeethShift` | `int` | `5` | Deslocamento para frente aplicado à série teeth. |
| `LipsPeriod` | `int` | `5` | Comprimento da média móvil lips. |
| `LipsShift` | `int` | `3` | Deslocamento para frente aplicado à série lips. |
| `MaType` | `MovingAverageType` | `Smoothed` | Algoritmo de média móvil usado para as três linhas do Alligator. |
| `AppliedPrice` | `AppliedPriceType` | `Median` | Preço da vela fornecido às médias móveis (fechamento, abertura, máxima, mínima, mediano, típico ou ponderado). |

### Conversão de pips

A estratégia multiplica as configurações de pip pelo `PriceStep` do ativo. Quando o instrumento usa 3 ou 5 casas decimais, o valor é ajustado em ×10 para imitar a definição de pip do MetaTrader para cotações fracionárias. Se nenhum passo de preço estiver disponível, assume-se um valor de 1.

## Notas de implementação

- `MaxPositions` atua sobre o tamanho agregado da posição porque o StockSharp opera em modo netting. Entradas adicionais aumentam o preço médio em vez de criar tickets de posição separados.
- O stop-loss e o take-profit são rastreados internamente e executados com ordens de mercado na primeira vela que viola os limites, correspondendo ao comportamento do especialista MQL original.
- O dimensionamento baseado em risco requer uma distância de stop-loss diferente de zero; caso contrário, o sistema recorre ao `OrderVolume` fixo.
- Todos os valores dos indicadores são atualizados apenas em velas concluídas (`CandleStates.Finished`) para evitar sinais prematuros.
