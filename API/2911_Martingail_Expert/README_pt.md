# Estratégia Martingail Expert
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Portagem do consultor especializado MetaTrader 5 **MartingailExpert.mq5**.
- Usa um cruzamento de oscilador estocástico com parâmetros configuráveis %K, %D e desaceleração para abrir posições.
- Implementa uma grade estilo martingale com entradas de média e rompimento que escalam o volume geometricamente.
- Projetado para portfólios em posição líquida — a estratégia mantém uma única posição comprada ou vendida agregada.

## Lógica de negociação
### Critérios de entrada
1. A estratégia processa velas fechadas do período `CandleType`.
2. Os valores estocásticos são tomados da vela anterior concluída para imitar a chamada MQL `iStochastic(..., 1)`.
3. Uma entrada comprada é acionada quando:
   - O %K anterior é maior que o %D anterior.
   - O %D anterior está acima de `BuyLevel`.
   - Não existe posição aberta.
4. Uma entrada vendida é acionada quando:
   - O %K anterior está abaixo do %D anterior.
   - O %D anterior está abaixo de `SellLevel`.
   - Não existe posição aberta.
5. Todas as ordens de mercado usam o valor `Volume` normalizado (arredondado para o `Security.VolumeStep` mais próximo).

### Escalamento de posição
- `ProfitPips` define a distância (em pips) necessária para adicionar outra posição base na direção do lucro.
  - Comprado: se a máxima da vela atingir `lastEntryPrice + ProfitPips * positionCount`, uma nova ordem com o `Volume` base é enviada.
  - Vendido: se a mínima da vela atingir `lastEntryPrice - ProfitPips * positionCount`, uma ordem base é enviada.
- `StepPips` define a distância de média (em pips) para aplicar o multiplicador martingale.
  - Comprado: se a mínima da vela tocar `lastEntryPrice - StepPips`, o próximo volume de ordem é `lastVolume * Multiplier`.
  - Vendido: se a máxima da vela tocar `lastEntryPrice + StepPips`, o mesmo dimensionamento martingale é aplicado.
- Cada operação executada atualiza `lastEntryPrice`, `lastVolume` e a contagem interna de posições ativas.

### Lógica de saída
- O preço do último trade executado é armazenado por direção.
- Se o preço atingir `lastEntryPrice ± ProfitPips` (usando máximas de vela para comprados e mínimas para vendidos), todas as posições abertas são fechadas por ordem de mercado.
- Uma vez que a posição agregada retorna a zero, as variáveis de estado martingale são redefinidas.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Volume` | `0.03` | Tamanho de lote base para a ordem inicial e complementos baseados em lucro. |
| `Multiplier` | `1.6` | Multiplicador martingale para entradas de média. |
| `StepPips` | `25` | Distância em pips que aciona ordens de média contra a tendência. |
| `ProfitPips` | `9` | Distância em pips usada tanto para saídas de lucro como para complementos de rompimento. |
| `KPeriod` | `5` | Período de lookback do cálculo estocástico %K. |
| `DPeriod` | `3` | Período de suavização da linha estocástica %D. |
| `Slowing` | `3` | Suavização aplicada à linha %K (estocástico lento). |
| `BuyLevel` | `20` | Valor mínimo de %D necessário para permitir entradas compradas. |
| `SellLevel` | `55` | Valor máximo de %D necessário para permitir entradas vendidas. |
| `CandleType` | período de 5 minutos | Período usado para construir velas e indicadores. |

## Notas de implementação
- A distância em pips é calculada a partir de `Security.PriceStep`. Instrumentos com cotações de 3 ou 5 decimais são automaticamente ajustados multiplicando o passo de preço por 10 para corresponder à lógica de pip MQL original.
- Os volumes são arredondados para baixo para o `Security.VolumeStep` mais próximo. Valores que ficam abaixo do passo mínimo negociável são ignorados.
- A estratégia depende das máximas e mínimas das velas para aproximar os gatilhos intra-barra porque a API de alto nível opera sobre velas concluídas.
- `OnOwnTradeReceived` rastreia os preços e volumes de execução reais para reproduzir fielmente a sequência de escalada martingale.

## Dicas de uso
- Alinhe `CandleType` com o período usado no modelo original do MetaTrader (comumente M5) para comportamento similar.
- Certifique-se de que os metadados do instrumento (passo de preço, passo de volume) estejam preenchidos; caso contrário, ajuste `Volume`, `StepPips` e `ProfitPips` manualmente para corresponder às especificações do corretor.
- Considere habilitar gerenciamento de risco externo (stops ou limites de capital) porque a lógica martingale aumenta intencionalmente a exposição durante movimentos adversos.

## Diferenças em relação ao consultor especializado original
- A versão StockSharp processa velas concluídas em vez de cada tick; as verificações de limiar usam máximas/mínimas de velas para aproximar o comportamento intra-barra.
- As verificações de margem de conta específicas do MetaTrader não estão disponíveis nas estratégias de alto nível do StockSharp; certifique-se de que o capital adequado esteja configurado externamente.
- A execução de ordens e o rastreamento de posições aproveitam o modelo de netting do StockSharp; o modo de cobertura não é suportado.
