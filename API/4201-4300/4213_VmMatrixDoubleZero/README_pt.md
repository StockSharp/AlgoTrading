# VmMatrix Duplo Zero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
VmMatrix Double Zero é uma porta StockSharp do MetaTrader 4 consultor especialista `vMATRIXDoubleZero`. O robô original procura rompimentos de "zero duplo" arredondando a vela anterior para perto de duas casas decimais e entrando em negociações quando o preço ultrapassa esse nível arredondado. A porta mantém a estrutura de filtro em camadas do EA: comparações configuráveis ​​de polarização de múltiplas barras, verificações opcionais de volume e faixa, uma porta de aceleração ATR e um filtro secundário de força de oscilação. A estratégia também pode exigir o Commodity Channel Index diário (CCI) para confirmar a direção e oferece um componente adaptativo de obtenção de lucro derivado de estatísticas horárias ATR.

A negociação é limitada a uma janela de tempo de terminal definida pelo usuário e botões separados controlam se configurações longas ou curtas podem ser feitas. Stops e metas são gerenciados internamente, incluindo uma aproximação do comportamento original do trailing stop que amplia o nível de take-profit sempre que o trailing estiver habilitado.

## Lógica estratégica
### Detecção de polarização
* **Rompimento arredondado** – o gatilho principal compara o fechamento das duas últimas velas concluídas com o fechamento anterior arredondado para duas casas decimais. Um sinal longo requer `Close[2] < round(Close[1], 2)` e `Close[1] > round(Close[1], 2)`; sinais curtos revertem as desigualdades.
* **Filtro de matriz (opcional)** – quando ativado, seis velas históricas definidas pelos parâmetros `LongK1…LongK6` (para posições compradas) ou `ShortK1…ShortK6` (para posições vendidas) são comparadas usando desvios de ponto médio. Cada desvio é calculado como `Close - (High + Low) / 2`. As comparações refletem o EA original e exigem que o primeiro desvio domine o segundo, o terceiro exceda um quarto na escala do multiplicador (`LongQc`/`ShortQc`) e o quinto exceda um segundo sexto na escala do multiplicador (`LongQg`/`ShortQg`).

### Filtros adicionais
* **Filtro de sessão** – as negociações só são avaliadas quando o horário de fechamento da vela processada fica entre `StartHour` e `EndHour` (inclusive).
* **Filtro de volume** – se ativado, o volume total da vela anterior deve exceder `MinimumVolume`.
* **Compressão de faixa** – a máxima mais alta e a mínima mais baixa das últimas `RangeBars` velas devem estar dentro de `RangeThresholdPips` pips.
* **ATR aceleração** – compara o valor ATR mais recente (duração `AtrPeriod` no período de trabalho) com o valor ATR de `AtrShift` barras atrás. O sinal é aceito somente se o ATR atual for maior, imitando a alternância do VSA do EA.
* **Filtro de oscilação secundário** – quando ativo, uma soma ponderada de diferenças altas/baixas construída a partir do lookback `SecondaryPivot` deve ser positiva para posições compradas ou negativa para posições vendidas. Os pesos (`Xb2`, `Xs2`, `Yb2`, `Ys2`) seguem o esquema de parâmetros original onde 50 representa neutralidade.
* **Confirmação diária CCI** – portão opcional que exige que o valor diário mais recente CCI (período `DailyCciPeriod`) esteja acima de zero para posições compradas ou abaixo de zero para posições vendidas.

### Gerenciamento de pedidos
* **Tamanho da entrada** – os pedidos usam `OrderVolume` ajustado à etapa de volume do título. Se uma posição oposta já estiver aberta, a estratégia opcionalmente a fecha primeiro (`CloseOnBiasFlip` deve ser verdadeira); caso contrário, a nova entrada será ignorada porque a porta é executada em um ambiente de rede.
* **Paradas iniciais** – as distâncias de stop-loss são expressas em pips até `LongStopLossPips`/`ShortStopLossPips` e convertidas usando o tamanho do pip detectado. As distâncias de lucro usam `LongTakeProfitPips`/`ShortTakeProfitPips` e podem ser aumentadas pelo componente dinâmico abaixo.
* **Take Profit dinâmico** – quando `UseDynamicTakeProfit` está ativado, a estratégia adiciona uma combinação ponderada de estatísticas horárias ATR e diferenças de swing à distância base de take-profit. A contribuição reflete a função `TPb()` do EA: ela combina a mudança horária ATR(1), a última horária ATR(1), horária ATR(25) e a diferença entre os máximos separados por barras `SwingPivot`. Todos os pesos estão centralizados em torno de 50, correspondendo à interface original.
* **Trailing stop** – ativar `UseTrailingStop` ativa um trailing stop em estilo de etapa que aumenta (ou diminui) o nível de stop sempre que o preço percorre aproximadamente o dobro da distância de parada configurada além da parada atual. Como na versão MQL, a distância do lucro é multiplicada por 10 para manter efetivamente a negociação aberta enquanto o trailing está ativo.
* **Saídas protetoras** – em cada vela finalizada a estratégia verifica se o stop-loss ou o take-profit foram violados. As posições são fechadas no mercado em resposta. Uma inversão de polarização (`CloseOnBiasFlip`) também fecha a posição atual se o sinal oposto for detectado.

## Parâmetros
A tabela a seguir resume os parâmetros expostos (todos estão disponíveis para otimização, salvo indicação em contrário):

| Grupo | Parâmetro | Descrição |
| --- | --- | --- |
| Geral | `StartHour` / `EndHour` | Janela de negociação inclusiva no horário do terminal. |
| Geral | `OrderVolume` | Tamanho base do pedido, normalizado para a etapa de volume do instrumento. |
| Geral | `UseTrailingStop` | Permite a aproximação do trailing stop e amplia o fator de lucro para emular o EA. |
| Geral | `CloseOnBiasFlip` | Se for verdade, fecha a exposição oposta antes de entrar numa nova negociação. |
| Longo / Curto | `EnableLongs` / `EnableShorts` | Alterna o processamento de sinal longo ou curto. |
| Longo / Curto | `LongStopLossPips`, `LongTakeProfitPips`, `ShortStopLossPips`, `ShortTakeProfitPips` | Distâncias de stop-loss e take-profit medidas em pips. |
| Filtros | `UseBiasFilter` with `LongK1…LongK6`, `ShortK1…ShortK6`, `LongQc`, `LongQg`, `ShortQc`, `ShortQg` | Configura as comparações de desvio no estilo de matriz para sinais longos e curtos. |
| Filtros | `UseRangeFilter`, `RangeBars`, `RangeThresholdPips` | Rejeita negociações quando a faixa de preço recente excede o limite do pip. |
| Filtros | `UseVolumeFilter`, `MinimumVolume` | Requer que o volume da vela anterior exceda o limite. |
| Filtros | `UseVsaFilter`, `AtrPeriod`, `AtrShift` | Exige que ATR tenha aumentado em relação a `AtrShift` barras atrás. |
| Filtros | `UseSecondaryFilter`, `Xb2`, `Xs2`, `Yb2`, `Ys2`, `SecondaryPivot` | Filtro ponderado de força de oscilação com base em altos e baixos. |
| Filtros | `UseDailyCciFilter`, `DailyCciPeriod` | Portão diário CCI; os comprados precisam de positivo CCI, os vendidos precisam de negativo CCI. |
| Obtenha lucro | `UseDynamicTakeProfit`, `WeightSn1…WeightSn4`, `SwingPivot` | Controla o componente adaptável de obtenção de lucro que combina métricas horárias ATR e distâncias de swing. |
| Geral | `CandleType` | Período primário que orienta todos os cálculos de sinal. |

## Notas adicionais
* O tamanho do pip é inferido de `Security.PriceStep`. Os símbolos FX de cinco e três dígitos são mapeados automaticamente para um multiplicador de 10×, espelhando o tratamento MQL de `Digits` e `Point`.
* A porta assina três fluxos de dados: o período de trabalho, velas horárias (para cálculos ATR) e velas diárias (para CCI). Certifique-se de que o provedor de dados possa fornecer todos os prazos solicitados.
* Como as estratégias StockSharp operam em posições líquidas, não há suporte para cobrir o mesmo instrumento em ambas as direções simultaneamente. Ative o `CloseOnBiasFlip` para imitar a capacidade do EA de fechar e reverter rapidamente.
* O comportamento do trailing-stop é aproximado; o EA usou valores de spread brutos para determinar a etapa final. O porto exige que o preço percorra aproximadamente o dobro da distância da parada antes de avançar a parada, o que produz um resultado semelhante sem informações explícitas de spread.
