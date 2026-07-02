# Estratégia Brandy v1.2 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Brandy v1.2** é uma conversão direta do consultor especialista MetaTrader 4 "Brandy_v1_2.mq4" na estrutura de estratégia de alto nível StockSharp. O sistema avalia um par de médias móveis simples deslocadas (SMAs) calculadas sobre o preço de fechamento da série de velas configurada. Novas posições são abertas apenas quando os SMAs de longo e curto prazo mostram impulso sincronizado na mesma direção, enquanto as negociações existentes são gerenciadas usando reversões de inclinação, níveis de stop-loss fixos e um módulo opcional de trailing stop.

O script MQL original foi executado exatamente uma vez por barra concluída. Esta porta processa StockSharp velas finalizadas da mesma maneira, garantindo que todas as decisões de negociação sejam baseadas em dados fechados, sem depender de barras parcialmente formadas.

## Lógica de negociação
1. **Preparação de indicadores**
   - Dois SMAs são calculados: uma linha de base mais longa (`LongPeriod`) e uma linha de confirmação mais curta (`ShortPeriod`).
   - Cada média é acessada duas vezes: o valor da barra anterior (shift = 1) e outro valor deslocado por `LongShift`/`ShortShift` barras respectivamente. Isso reproduz as chamadas `iMA(..., shift)` presentes no EA original.
2. **Regras de entrada**
   - **Compre** quando o valor da barra anterior de ambos os SMAs for maior do que suas contrapartes deslocadas (ambas as inclinações apontando para cima) e nenhuma posição estiver aberta.
   - **Venda** quando o valor da barra anterior de ambos os SMAs for inferior aos seus homólogos deslocados (ambas as inclinações apontando para baixo) e nenhuma posição estiver aberta.
   - Apenas uma posição pode estar ativa a qualquer momento, espelhando a verificação `k == 0` na fonte MQL.
3. **Regras de saída**
   - **Reversão de inclinação**: uma posição longa aberta é liquidada se a posição longa SMA cair (`longPrev < longShifted`), enquanto uma posição curta é coberta quando a posição longa SMA subir (`longPrev > longShifted`).
   - **Stop-loss fixo**: ao entrar, a estratégia armazena um nível de stop inicial compensado em `StopLossPoints × PriceStep` do preço de entrada. O stop é verificado em relação à faixa máxima/mínima da vela, aproximando-se do gerenciamento do nível de tick do consultor original.
   - **Trailing stop**: se `TrailingStopPoints ≥ 100`, a estratégia replica a lógica final (parâmetro `ts`). Quando o lucro flutuante excede a distância final, o stop é puxado para `currentPrice ± trailingDistance`, desde que o novo nível esteja mais próximo do preço do que o stop existente. Este comportamento corresponde às chamadas `OrderModify` no especialista MQL.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `LongPeriod` | 70 | Comprimento do SMA primário (`p1` em MQL). Deve ser > 0. |
| `LongShift` | 5 | Deslocamento para trás aplicado à comparação longa SMA (`s1`). Pode ser zero. |
| `ShortPeriod` | 20 | Comprimento da confirmação SMA (`p2`). Deve ser > 0. |
| `ShortShift` | 5 | Mudança para trás para o SMA curto (`s2`). Pode ser zero. |
| `StopLossPoints` | 50 | Distância de parada fixa em etapas de preço (`sl`). Defina como 0 para desativar a parada brusca. |
| `TrailingStopPoints` | 150 | Distância final em etapas de preço (`ts`). O rastreamento é ativado somente quando o valor é ≥ 100, espelhando o limite original. |
| `Volume` | 0,1 | Volume do pedido usado para entradas (`lots`). |
| `CandleType` | Período de 15 minutos | Série de velas processadas pela estratégia (configurável pelo usuário). |

### Dependência da etapa de preço
Ambos os parâmetros de parada operam em pontos do instrumento. O método auxiliar os converte em deltas de preços absolutos por meio de `Security.PriceStep`. Se a fonte de dados não fornecer `PriceStep`, a estratégia volta para `0.0001` para que a lógica continue funcionando, embora com uma conversão aproximada. Sempre verifique os metadados do símbolo em StockSharp antes do uso ao vivo.

## Gestão de risco
- **Parada brusca**: armazenada internamente e validada em cada vela finalizada. Quando o preço viola o stop, a chamada `SellMarket`/`BuyMarket` correspondente fecha toda a posição.
- **Trailing stop**: segue as condições exatas do EA original, movendo o stop somente quando o lucro atual excede a distância final *e* o stop existente ainda está mais longe que essa distância.
- **Posição única**: o algoritmo nunca faz pirâmides; ele tem uma única posição longa, uma única posição curta ou é plano.

## Notas de implementação
- O estado (preço de entrada, nível de stop, históricos de SMA) é redefinido automaticamente em `OnReseted()`, garantindo backtests e reinicializações limpos.
- Os históricos dos indicadores são armazenados em buffers rolantes curtos para reproduzir os deslocamentos `iMA(..., shift)` sem chamar `GetValue()`.
- Todos os comentários embutidos permanecem em inglês, conforme exigido pelas diretrizes do repositório.
- Nenhuma contraparte Python é fornecida. Somente a implementação de alto nível C# é entregue em `CS/BrandyV12Strategy.cs` conforme solicitado.

## Uso
1. Coloque a estratégia em uma solução StockSharp, selecione o instrumento desejado e certifique-se de que os dados da vela correspondam ao período especificado por `CandleType`.
2. Configure os parâmetros na UI ou por meio de código. Os padrões replicam os valores MT4 originais.
3. Comece a estratégia. Ele assinará a série de velas, desenhará ambos os SMAs no gráfico e gerenciará as negociações automaticamente.

> **Isenção de responsabilidade:** Esta porta destina-se a fins educacionais e de teste. Sempre valide o comportamento em sessões de negociação históricas e em papel antes de implantar em mercados reais.
