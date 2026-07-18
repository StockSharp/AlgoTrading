# Combo EA4 FSF R Atualizado 5 Estratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão StockSharp do MetaTrader consultor especialista "Combo_EA4FSFrUpdated5". Ele combina cinco módulos técnicos diferentes – médias móveis, RSI, oscilador estocástico, parabólico SAR e atraso zero MACD – para validar cada decisão de negociação. Uma posição é aberta somente quando **todos** os módulos habilitados apontam para a mesma direção, recriando a lógica de consenso estrito do EA original. O gerenciamento opcional de rastreamento, saídas automáticas baseadas em sinais e a capacidade de virar na direção oposta após o fechamento também são preservados.

## Pilha de indicadores
- **Médias móveis** – Três médias configuráveis (MA1, MA2, MA3) com buffers baseados em ATR que reduzem sinais falsos de cruzamento. Cinco modos de agregação diferentes replicam as opções "MA_MODE" do EA.
- **Índice de força relativa (RSI)** – Vários modos de confirmação, incluindo sobrecompra/sobrevenda clássica, detecção de tendência baseada em inclinação, modo combinado e validação baseada em zona.
- **Stochastic oscilador** – Comprimentos rápidos/lentos/desacelerados com filtragem de banda alta/baixa opcional.
- **Parabolic SAR** – Fornece uma verificação de polaridade da tendência em relação ao fechamento da vela anterior.
- **Zero-lag MACD** – Usa médias móveis exponenciais com atraso zero para corresponder ao indicador `ZeroLag_MACD.mq4` agrupado. Suporta três modos de sinal (estrutura de tendência, cruzamento de linha zero ou combinado).
- **Average True Range (ATR)** – Impulsiona as distâncias de stop-loss/take-profit e os buffers de cruzamento MA.

## Lógica de negociação
### Condições de entrada
1. Os valores dos indicadores de todos os módulos habilitados devem estar disponíveis (a estratégia aguarda automaticamente o aquecimento).
2. Para cada módulo habilitado, uma direção de alta ou baixa é calculada de acordo com seu modo:
   - **Médias móveis** – combinações MA1/MA2/MA3 com buffers ATR para confirmar mudanças de direção.
   - **RSI** – Quatro modos que abrangem limites, impulso e lógica de zona.
   - **Stochastic** – Confirmação cruzada K/D com filtros altos/baixos opcionais.
   - **Parabolic SAR** – Requer que o preço esteja acima/abaixo do valor SAR da vela anterior.
   - **Zero-lag MACD** – alinhamento de tendência, confirmação cruzada de linha zero ou ambos.
3. Se **cada** módulo habilitado retornar `Buy`, a estratégia envia uma ordem de compra a mercado. Se cada módulo retornar `Sell`, uma ordem de venda a mercado será emitida. Caso contrário, nenhuma negociação será aberta.

### Condições de saída
- **Saídas baseadas em sinal** – Quando `AutoClose` está ativado, a mesma lógica de consenso é avaliada usando sinalizadores de saída dedicados (`UseMaClosing`, `UseMacdClosing`, etc.). Uma posição longa é fechada quando todos os módulos de saída habilitados concordam com um sinal de baixa; uma posição curta é fechada quando eles concordam com um sinal de alta. Se `OpenOppositeAfterClose` for verdadeiro, a posição oposta será enfileirada imediatamente após o preenchimento de fechamento.
- **Níveis de proteção** – Os níveis iniciais de stop-loss e take-profit são derivados do valor atual de ATR (`AtrPeriod`) multiplicado por `AtrMultiplier`. O buffer pip do EA é emulado com o tamanho do passo do instrumento. As negociações longas usam `ATR × multiplier − buffer` para stops e `ATR × multiplier + buffer` para metas (espelhadas para vendas).
- **Trailing stop** – Quando `UseTrailingStop` está ativado, o preço stop é ajustado em cada vela finalizada usando a distância do ponto configurada (`TrailingStop`).
- **Saídas difíceis** – Se o preço atingir a intrabar stop-loss ou take-profit, a posição é fechada imediatamente e nenhuma entrada oposta é acionada.

### Dimensionamento de posição
- **Modo estático** – Quando `UseStaticVolume` é verdadeiro, as negociações são feitas com o parâmetro `StaticVolume` fixo.
- **Modo dinâmico** – Caso contrário, a estratégia deriva um tamanho aproximado do valor atual do portfólio e `RiskPercent`, voltando à base `Volume` se os dados do portfólio ou de preços não estiverem disponíveis.

## Parâmetros
| Grupo | Parâmetro | Descrição |
|-------|-----------|-------------|
| Entradas | `UseMa` | Ative a confirmação da média móvel. |
| Entradas | `MaMode` | Seleciona a combinação MA (rápido/médio, médio/lento, combinado, etc.). |
| Indicadores | `Ma1Period`, `Ma2Period`, `Ma3Period` | Períodos das três médias móveis. |
| Indicadores | `Ma1BufferPeriod`, `Ma2BufferPeriod` | período ATRs usados como buffer para verificações cruzadas de MA. |
| Indicadores | `Ma1Method`, `Ma2Method`, `Ma3Method` | Tipos de cálculo de média móvel (SMA, EMA, SMMA, LWMA). |
| Indicadores | `Ma1Price`, `Ma2Price`, `Ma3Price` | Preço aplicado para cada média móvel. |
| Entradas | `UseRsi` | Ative a confirmação RSI. |
| Indicadores | `RsiPeriod` | RSI período de cálculo. |
| Entradas | `RsiMode` | Modo de confirmação RSI (sobrecompra/sobrevenda, tendência, combinado, zona). |
| Entradas | `RsiBuyLevel`, `RsiSellLevel` | Limites para lógica de sobrevenda/sobrecompra. |
| Entradas | `RsiBuyZone`, `RsiSellZone` | Limites de zona para o modo 4. |
| Entradas | `UseStochastic` | Ative a confirmação estocástica. |
| Indicadores | `StochasticK`, `StochasticD`, `StochasticSlowing` | Parâmetros K/D/lento. |
| Entradas | `UseStochasticHighLow` | Exigir que o estocástico rompa as bandas altas/baixas configuradas. |
| Entradas | `StochasticHigh`, `StochasticLow` | Limiares estocásticos superiores e inferiores. |
| Entradas | `UseSar` | Ative a confirmação parabólica SAR. |
| Indicadores | `SarStep`, `SarMax` | SAR configurações de aceleração. |
| Entradas | `UseMacd` | Ative a confirmação de atraso zero MACD. |
| Indicadores | `MacdFast`, `MacdSlow`, `MacdSignal` | Parâmetros MACD. |
| Indicadores | `MacdPrice` | Preço aplicado para MACD. |
| Entradas | `MacdMode` | MACD modo de confirmação. |
| Risco | `UseTrailingStop`, `TrailingStop` | Alternância e distância do trailing stop (em pontos). |
| Risco | `UseStaticVolume`, `StaticVolume`, `RiskPercent` | Controles de dimensionamento de posição. |
| Risco | `AtrPeriod`, `AtrMultiplier` | ATR configurações para gerenciamento de riscos. |
| Saídas | `AutoClose` | Habilite a lógica de consenso de saída. |
| Saídas | `OpenOppositeAfterClose` | Vire na direção oposta após uma saída baseada em sinal. |
| Saídas | `UseMaClosing`, `MaModeClosing` | Configuração de saída média móvel. |
| Saídas | `UseMacdClosing`, `MacdModeClosing` | MACD configuração de saída. |
| Saídas | `UseRsiClosing`, `RsiModeClosing` | RSI configuração de saída. |
| Saídas | `UseStochasticClosing` | Stochastic alternância de saída. |
| Saídas | `UseSarClosing` | SAR alternância de saída. |
| Geral | `CandleType` | Período primário (velas padrão de 5 minutos). |

## Notas
- A estratégia opera uma posição líquida por vez (longa, curta ou plana), espelhando a restrição de "máximo de pedidos iguais" de MetaTrader com uma abordagem mais simples e amigável StockSharp.
- As entradas opostas pendentes são enfileiradas apenas para saídas baseadas em sinal e são ignoradas se um stop-loss ou take-profit fechar a negociação.
- Como os requisitos de margem da conta são específicos da corretora, o dimensionamento dinâmico da posição utiliza uma fórmula aproximada baseada no risco; verifique o volume resultante antes da implantação ativa.
- Certifique-se de que os indicadores de atraso zero MACD e ATR tenham histórico de aquecimento suficiente antes de esperar negociações, assim como no EA original.
