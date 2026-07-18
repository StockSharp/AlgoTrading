# Estratégia de Rompimento de Canal com uma MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A **Estratégia de Rompimento de Canal com uma MA** replica o consultor especialista MetaTrader 5 *One MA EA* usando a API de estratégia de alto nível do StockSharp. O sistema desenha uma média móvel deslocada e a rodeia com um canal configurável baseado em pips. Quando o preço abre fora do canal após testá-lo na mesma barra, a estratégia abre uma posição na direção do rompimento enquanto as proteções opcionais de stop-loss e take-profit gerenciam o risco automaticamente.

Características principais:
- Suporta múltiplos métodos de cálculo de média móvel (SMA, EMA, SMMA, LWMA).
- Permite escolher o preço da vela (fechamento, abertura, máximo, mínimo, mediana, típico, ponderado) que alimenta a média móvel.
- Aplica deslocamentos independentes ao valor da média móvel e à vela usada para avaliação de sinais, correspondendo aos controles `Current Bar` do EA original.
- Converte distâncias em pips para incrementos de preço absolutos usando o `PriceStep` do instrumento e a precisão decimal (instrumentos de 3/5 decimais mapeiam automaticamente para pips FX clássicos).

## Lógica de Trading
1. **Preparação do indicador**
   - Uma média móvel com período `MaPeriod`, método `MaMethodParam`, deslocamento `MaShift` e preço aplicado `AppliedPriceType` é calculada a partir da série de velas inscrita (`CandleType`).
   - Os offsets do canal são convertidos de pips para incrementos de preço: `ChannelHighPips` acima e `ChannelLowPips` abaixo da média móvel deslocada.
   - Buffers históricos permitem referenciar barras anteriores (`MaBarShift` para a série de MA, `PriceBarShift` para dados OHLC) exatamente como na versão MQL.

2. **Geração de sinais**
   - **Rompimento altista**: a mínima da vela inspecionada permanece entre a linha de base da MA e o canal superior, enquanto sua abertura aparece acima do canal superior. Se não há exposição comprada (`Position <= 0`), a estratégia compra.
   - **Rompimento baixista**: a máxima da vela inspecionada permanece entre a linha de base da MA e o canal inferior, enquanto sua abertura aparece abaixo do canal inferior. Se não há exposição vendida (`Position >= 0`), a estratégia vende.
   - O volume da ordem equivale ao `TradeVolume` configurado mais qualquer quantidade necessária para zerar uma posição oposta, refletindo o comportamento hedge-to-net do especialista fonte.

3. **Gestão de risco**
   - `StopLossPips` e `TakeProfitPips` são traduzidos em distâncias de preço absolutas e passados a `StartProtection`, habilitando ordens de saída automatizadas para cada posição.
   - Com valores zero, a ordem protetora respectiva é desabilitada.

Nenhuma lógica de saída adicional é aplicada; as posições fecham apenas através do módulo de proteção ou revertendo para o sinal oposto.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `MaPeriod` | Comprimento da média móvel. Deve ser > 0. |
| `MaShift` | Deslocamento horizontal da média móvel em barras. Valores positivos deslocam a MA para a direita. |
| `MaMethodParam` | Tipo de cálculo da média móvel (`Sma`, `Ema`, `Smma`, `Lwma`). |
| `AppliedPriceType` | Preço da vela alimentado na MA (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |
| `MaBarShift` | Qual valor histórico de MA usar (0 = barra atual processada). |
| `PriceBarShift` | Qual vela histórica inspecionar para valores OHLC. |
| `ChannelHighPips` | Distância (em pips) da MA ao limite superior do canal. |
| `ChannelLowPips` | Distância (em pips) da MA ao limite inferior do canal. |
| `StopLossPips` | Distância do stop protetor em pips. Zero desabilita o stop. |
| `TakeProfitPips` | Distância do alvo de lucro em pips. Zero desabilita o alvo. |
| `TradeVolume` | Tamanho da ordem em unidades de volume de estratégia (mapeado para `Strategy.Volume`). |
| `CandleType` | Série de dados de velas usada para cálculos e sinais. |

## Notas de Implementação
- A conversão de pip para preço usa `PriceStep` e `Decimals`. Para símbolos com 3 ou 5 decimais, o valor do pip equivale a `PriceStep * 10`, caso contrário equivale a `PriceStep`.
- Os buffers históricos são implementados com janelas deslizantes de tamanho fixo para que a estratégia possa acessar barras por índice sem depender de chamadas `GetValue` do indicador, cumprindo as diretrizes do projeto.
- A estratégia baseia-se exclusivamente em velas terminadas; velas não terminadas são ignoradas para evitar sinais prematuros.
- O renderizador opcional de gráfico desenha velas de preço e trades executados quando uma área de gráfico está disponível na aplicação host.

## Dicas de Uso
- Garantir que o instrumento inscrito exponha dados válidos de `PriceStep`/`Decimals`; caso contrário, ajustar os parâmetros baseados em pips manualmente.
- Otimizar `MaPeriod`, distâncias do canal e deslocamentos de barras para adaptar o comportamento de rompimento a mercados ou períodos específicos.
- Combinar com controles de risco em nível de portfólio quando implantado ao vivo, pois a estratégia sempre tem uma posição líquida por instrumento.
