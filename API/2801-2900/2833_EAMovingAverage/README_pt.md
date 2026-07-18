# Estratégia EA Moving Average
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Convertida do consultor especialista MetaTrader **"EA Moving Average"** (edição barabashkakvn).
- Usa quatro médias móveis independentes para controlar entradas e saídas compradas e vendidas.
- Projetada para um único símbolo em modo netting. O tipo de candle padrão é o período de 15 minutos, mas qualquer tipo de candle regular pode ser selecionado.
- A estratégia abre no máximo uma posição por vez. Enquanto uma posição está ativa, apenas as regras de saída são avaliadas.

## Lógica de trading
### Entrada comprada
1. O candle atual deve fechar acima da média móvel *Buy Open* após abrir abaixo dela (cruzamento real dentro de uma única barra).
2. `UseBuy` deve estar habilitado.
3. Se `ConsiderPriceLastOut` estiver habilitado, o preço atual deve ser menor ou igual ao preço da última negociação fechada. Isso evita comprar acima da saída mais recente.
4. Quando as condições são satisfeitas, a estratégia envia uma ordem de compra a mercado dimensionada pelo modelo de risco.

### Saída comprada
1. Ativa apenas enquanto a posição líquida é comprada.
2. O candle deve abrir acima da média móvel *Buy Close* e fechar de volta abaixo dela, sinalizando um cruzamento de baixa.
3. Quando acionada, toda a posição é fechada com uma ordem a mercado.

### Entrada vendida
1. O candle deve fechar abaixo da média móvel *Sell Open* após abrir acima dela.
2. `UseSell` deve estar habilitado.
3. Se `ConsiderPriceLastOut` estiver habilitado, o preço atual deve ser maior ou igual ao último preço de saída. Isso evita vender abaixo da cobertura anterior.
4. Uma ordem de venda a mercado é enviada usando o volume baseado em risco.

### Saída vendida
1. Ativa apenas enquanto a posição é vendida.
2. O candle deve abrir abaixo da média móvel *Sell Close* e fechar acima dela.
3. A posição vendida é totalmente coberta a mercado.

## Risco e dimensionamento de posição
- `MaximumRisk` expressa o capital de risco por negociação como uma fração do patrimônio líquido do portfólio. A estratégia divide esse valor de risco pelo preço atual para obter uma estimativa de volume bruto.
- `DecreaseFactor` emula a redução de lote original do MetaTrader. Após duas ou mais negociações perdedoras consecutivas, o volume é reduzido proporcionalmente à sequência de perdas dividida por `DecreaseFactor`.
- Os volumes são alinhados ao passo de volume do instrumento e nunca caem abaixo de um passo. Se o cálculo de risco falhar, o fallback é a propriedade `Volume` da estratégia (padrão 1 contrato/lote).

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `MaximumRisk` | `0.02` | Fração do patrimônio arriscado por negociação. |
| `DecreaseFactor` | `3` | Fator de redução de lote após perdas consecutivas. Use `0` para desabilitar. |
| `BuyOpenPeriod` | `30` | Período da média móvel usada para entradas compradas. |
| `BuyOpenShift` | `3` | Deslocamento para frente (barras) aplicado à média móvel de entrada comprada. |
| `BuyOpenMethod` | `Exponential` | Método de média móvel para entradas compradas (`Simple`, `Exponential`, `Smoothed`, `LinearWeighted`). |
| `BuyOpenPrice` | `Close` | Entrada de preço para a média móvel de entrada comprada. |
| `BuyClosePeriod` | `14` | Período da média móvel de saída comprada. |
| `BuyCloseShift` | `3` | Deslocamento (barras) aplicado à média móvel de saída comprada. |
| `BuyCloseMethod` | `Exponential` | Método da média móvel de saída comprada. |
| `BuyClosePrice` | `Close` | Entrada de preço para a média móvel de saída comprada. |
| `SellOpenPeriod` | `30` | Período da média móvel de entrada vendida. |
| `SellOpenShift` | `0` | Deslocamento (barras) aplicado à média móvel de entrada vendida. |
| `SellOpenMethod` | `Exponential` | Método da média móvel de entrada vendida. |
| `SellOpenPrice` | `Close` | Entrada de preço para a média móvel de entrada vendida. |
| `SellClosePeriod` | `20` | Período da média móvel de saída vendida. |
| `SellCloseShift` | `2` | Deslocamento (barras) aplicado à média móvel de saída vendida. |
| `SellCloseMethod` | `Exponential` | Método da média móvel de saída vendida. |
| `SellClosePrice` | `Close` | Entrada de preço para a média móvel de saída vendida. |
| `UseBuy` | `true` | Habilitar ou desabilitar negociações compradas. |
| `UseSell` | `true` | Habilitar ou desabilitar negociações vendidas. |
| `ConsiderPriceLastOut` | `true` | Exigir melhoria de preço em relação à última saída antes de re-entrar. |
| `CandleType` | Período 15m | Série de candles usada para cálculos. |

## Notas adicionais
- O último preço de saída e o contador de perdas consecutivas são rastreados a partir das execuções de negociação, espelhando o comportamento do MetaTrader.
- Como o StockSharp executa em candles terminados, o filtro de preço de entrada compara com o preço de fechamento do candle, o que aproxima a comparação original de ask/bid baseada em ticks.
- A estratégia assume uma conta de netting; a cobertura de múltiplas posições simultaneamente não é suportada.
- Sempre valide a configuração com testes históricos antes de negociar com capital real.
