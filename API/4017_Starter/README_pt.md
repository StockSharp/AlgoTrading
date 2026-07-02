# Estratégia Inicial 2005
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Starter 2005** é uma conversão StockSharp de alto nível API do clássico MetaTrader 4 consultor especialista `Starter.mq4` lançado em 2005. O sistema original misturava um oscilador Laguerre, um filtro de inclinação de média móvel exponencial e uma confirmação de índice de canal de commodities. Esta porta mantém a árvore de decisão intacta enquanto adapta o gerenciamento de dinheiro e a execução às convenções StockSharp:

- Um proxy Laguerre RSI replica o buffer `iCustom("Laguerre")` que oscila entre 0 e 1.
- Um EMA de 5 períodos calculado sobre o preço médio fornece a mesma confirmação de inclinação ascendente/descendente usada pelo especialista MT4.
- Um CCI de 14 períodos medido nos preços de fechamento filtra configurações fracas, assim como a variável `Alpha` original.
- A rotina adaptativa de dimensionamento de lote reflete a função histórica `LotsOptimized()`, incluindo reduções baseadas em sequências após perdas consecutivas.
- As saídas de posição são acionadas pela reversão de Laguerre para fora da zona extrema ou pela negociação atingindo uma distância de lucro configurável equivalente a `Point * Stop`.

## Lógica de negociação
1. **Preparação de indicadores**
   - O valor de Laguerre RSI é reconstruído através de um filtro Laguerre de quatro estágios com `Gamma` configurável.
   - O comprimento de EMA é padrão para cinco velas e opera em `(High + Low) / 2` para corresponder a `PRICE_MEDIAN` em MQL4.
   - O período CCI é padronizado como 14 nos preços de fechamento, e um limite muito pequeno (`±5`) é mantido para permanecer fiel ao código legado.
2. **Configuração longa**
   - Laguerre deve ficar próximo de zero (`LaguerreEntryTolerance` emula a comparação estrita `== 0`).
   - EMA deve estar subindo em comparação com a vela finalizada anterior.
   - CCI deve ficar abaixo de `-CciThreshold`.
3. **Configuração curta**
   - Laguerre deve sentar-se perto de um (`1 - LaguerreEntryTolerance` aproxima-se de `== 1`).
   - EMA deve estar caindo.
   - CCI deve subir acima de `+CciThreshold`.
4. **Saídas**
   - As posições compradas fecham quando Laguerre sobe acima de `LaguerreExitHigh` (padrão `0.9`) ou quando o preço avança `TakeProfitPoints * PriceStep` a partir da entrada.
   - As vendas fecham quando Laguerre cai abaixo de `LaguerreExitLow` (padrão `0.1`) ou quando o preço cai na mesma distância.
   - Qualquer outra posição plana manual redefine automaticamente o estado interno para evitar dados de entrada obsoletos.

## Gestão de dinheiro
O auxiliar `CalculateOrderVolume` reproduz o comportamento `LotsOptimized()` do MT4:

1. **Dimensionamento baseado em risco** – O patrimônio líquido multiplicado por `MaximumRisk` é dividido por `RiskDivider` (padrão 500, como na regra original `/500`). Quando dividido pelo preço atual, resulta o tamanho do lote ajustado ao risco.
2. **Lote substituto** – Se o dimensionamento do risco produzir um número menor que `BaseVolume`, o algoritmo mantém o lote base.
3. **Redução da sequência de perdas** – Após duas ou mais negociações consecutivas com perdas, o volume é reduzido em `volume * losses / DecreaseFactor`, correspondendo exatamente ao loop MQL que inspecionou o histórico de negociações.
4. **Normalização** – Os volumes são normalizados para o `VolumeStep` do instrumento e fixados entre `MinVolume` e `MaxVolume` para evitar pedidos rejeitados.

O rastreamento de perdas consecutivas é redefinido após qualquer saída lucrativa e incrementos após perdas nas negociações; os resultados do ponto de equilíbrio deixam o contador intocado, espelhando o comportamento original que ignorou os tickets de lucro zero.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `BaseVolume` | `decimal` | `1.2` | Tamanho mínimo do lote utilizado quando o dimensionamento do risco sugere um valor menor. |
| `MaximumRisk` | `decimal` | `0.036` | Fração do patrimônio líquido exposta em uma nova posição antes da aplicação do divisor. |
| `RiskDivider` | `decimal` | `500` | Divisor aplicado ao capital de risco, reproduzindo a regra original `AccountFreeMargin() * MaximumRisk / 500`. |
| `DecreaseFactor` | `decimal` | `2` | Divisor de sequência usado para diminuir o volume após perdas consecutivas. |
| `MaPeriod` | `int` | `5` | Comprimento de EMA no preço médio da vela. |
| `CciPeriod` | `int` | `14` | Retrospectiva do índice de canais de commodities. |
| `CciThreshold` | `decimal` | `5` | Nível absoluto CCI necessário para acionar um sinal. |
| `LaguerreGamma` | `decimal` | `0.66` | Fator de suavização do filtro Laguerre. |
| `LaguerreEntryTolerance` | `decimal` | `0.02` | Tolerância em torno de 0/1 usada para imitar as verificações de igualdade originais. |
| `LaguerreExitHigh` | `decimal` | `0.9` | Nível de saída superior para posições longas. |
| `LaguerreExitLow` | `decimal` | `0.1` | Nível de saída mais baixo para posições curtas. |
| `TakeProfitPoints` | `decimal` | `10` | Meta de lucro expressa em faixas de preço (`Point * Stop` em MQL). |
| `CandleType` | `DataType` | `TimeFrame(5m)` | Assinatura de velas processada pela estratégia. |

## Notas de implementação
- Laguerre RSI é implementado em linha usando a recursão de quatro níveis do indicador original; nenhuma chamada para `GetValue()` é necessária.
- Os indicadores EMA e CCI são atualizados manualmente dentro do callback da vela para garantir que o feed de preço médio corresponda à opção `PRICE_MEDIAN` de MetaTrader.
- As entradas no mercado respeitam as sinalizações `AllowLong()` / `AllowShort()` e garantem que nenhuma ordem ativa esteja pendente, preservando o design de posição única da fonte EA.
- O rastreamento dos resultados comerciais usa o preço de decisão da vela (último preço, fechamento ou abertura) para estimar a direção do PnL e manter o contador da seqüência de perdas.
- Os comentários embutidos em inglês descrevem todos os principais blocos de decisão para ajudar na manutenção futura.

## Dicas de uso
- O EA original foi planejado para gráficos FX intradiários; comece com instrumentos líquidos que oferecem pequenas etapas de preço para que a meta de lucro de 10 pontos se alinhe com um pip.
- Como o script MT4 mantém apenas uma posição, execute a estratégia em ambientes onde o preenchimento parcial e os pedidos simultâneos são improváveis (testes históricos ou mercados líquidos).
- Ajuste `LaguerreEntryTolerance` se o oscilador raramente tocar exatamente 0 ou 1 em seu conjunto de dados.
- Ajuste `RiskDivider` e `DecreaseFactor` juntos para equilibrar o crescimento do risco e a mitigação de perdas.
