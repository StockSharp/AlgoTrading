# Estratégia EMA LWMA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A **Estratégia EMA LWMA RSI** reproduz o consultor especialista MetaTrader "EMA LWMA RSI" no StockSharp. Ela compara duas médias móveis que usam o mesmo preço aplicado e opcionalmente um deslocamento para frente, enquanto um filtro de Índice de Força Relativa confirma o momentum. O algoritmo reage apenas a velas recém-terminadas do período configurado e negocia uma única posição líquida: fecha qualquer exposição oposta antes de abrir uma nova ordem na direção sinalizada. As distâncias de stop-loss e take-profit são configuradas em pips e automaticamente escaladas para o tamanho de tick do instrumento.

## Lógica de Negociação
1. Calcular uma média móvel exponencial (EMA) e uma média móvel ponderada linear (LWMA) com comprimentos individuais mas o mesmo preço aplicado. Se `MaShift` for maior que zero, ambas as médias são deslocadas para frente pelo número especificado de barras para refletir o argumento "shift" do MetaTrader.
2. Processar um RSI com seu próprio preço aplicado. A estratégia usa o limiar clássico de 50 para distinguir momentum altista e baixista.
3. Quando uma vela terminada chega:
   - Um sinal de **compra** é gerado se o EMA cruza **acima** do LWMA (o EMA anterior era maior que o LWMA anterior, mas o EMA atual está abaixo do LWMA atual) e o valor RSI está **acima de 50**.
   - Um sinal de **venda** é gerado se o EMA cruza **abaixo** do LWMA (o EMA anterior era menor que o LWMA anterior, mas o EMA atual está acima do LWMA atual) e o valor RSI está **abaixo de 50**.
4. Os sinais definem flags de pendência internos. Antes de reverter, a estratégia primeiro fecha a posição existente com `ClosePosition()`. Após a confirmação do preenchimento, imediatamente envia uma ordem de mercado na direção solicitada.
5. As ordens protetoras são iniciadas via `StartProtection`. Se um stop-loss ou take-profit estiver desativado (definido como zero), essa parte é omitida, correspondendo ao comportamento MQL.

## Notas de Implementação
- A seleção de preço aplicado suporta as opções do MetaTrader (Fechamento, Abertura, Máxima, Mínima, Mediano, Típico, Ponderado, Médio). O preço ponderado é calculado como `(Máxima + Mínima + 2 * Fechamento) / 4`, idêntico a `PRICE_WEIGHTED`.
- O dimensionamento de pips multiplica automaticamente o `PriceStep` do instrumento por 10 para símbolos forex de 3/5 dígitos, garantindo que um pip equivalha a 10 pontos em cotações fracionárias.
- Os vínculos de indicadores dependem da assinatura de velas de alto nível do StockSharp. O tratamento de deslocamento usa indicadores `Shift` em vez de indexação manual de buffers.
- O código mantém flags booleanas para solicitações de compra/venda pendentes. Elas previnem ordens duplicadas enquanto o comando anterior ainda está pendente.
- Os auxiliares de gráfico desenham ambas as médias móveis no painel de preço e o RSI em uma área separada para inspeção visual.

## Parâmetros
| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `1h TimeFrame` | Série de velas processada pela estratégia. |
| `StopLossPips` | `int` | `150` | Distância de stop-loss em pips. `0` desativa o stop. |
| `TakeProfitPips` | `int` | `150` | Distância de take-profit em pips. `0` desativa o alvo. |
| `EmaPeriod` | `int` | `28` | Período da média móvel exponencial. |
| `LwmaPeriod` | `int` | `8` | Período da média móvel ponderada linear. |
| `MaShift` | `int` | `0` | Deslocamento para frente (barras) aplicado a ambas as médias móveis. |
| `RsiPeriod` | `int` | `14` | Período de médio do RSI. |
| `MaAppliedPrice` | `AppliedPriceType` | `Weighted` | Preço aplicado encaminhado para EMA e LWMA. |
| `RsiAppliedPrice` | `AppliedPriceType` | `Weighted` | Preço aplicado usado pelo RSI. |

## Uso
1. Anexar a estratégia ao instrumento desejado e definir `CandleType` para corresponder ao período usado no MetaTrader.
2. Ajustar as proteções baseadas em pips e as configurações de indicadores se o broker usar padrões diferentes.
3. Habilitar a negociação assim que a assinatura estiver ativa. A estratégia gerenciará uma posição por vez e usará `ClosePosition()` antes de mudar de direção.

Ainda não há tradução em Python disponível para esta estratégia.
