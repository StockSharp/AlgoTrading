# Estratégia de probabilidade do padrão Gselector
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **Gselector Pattern Probability** é uma versão StockSharp do especialista MetaTrader 4 "Gselector". Ele estuda mudanças de direção de séries de preços sintéticos construídas a partir de vários tamanhos de passos, mantém estatísticas de probabilidade para cada padrão observado e negocia quando a probabilidade de um movimento de continuação é alta o suficiente. As distâncias de stop-loss e take-profit são simuladas em software para refletir o comportamento original do especialista.

## Processo de aprendizagem
1. **Escadas sintéticas** – Para cada delta múltiplo configurado, a estratégia constrói uma série baseada em etapas, registrando o último preço de fechamento sempre que o mercado se move na distância necessária.
2. **Codificação de padrão** – Uma máscara de bits é criada comparando cada par de valores vizinhos dentro da escada. As etapas ascendentes recebem o bit `0`, as etapas descendentes recebem o bit `1`, que reproduz a codificação `Ncomb` da implementação MQL.
3. **Rastreamento de eventos** – Quando um novo padrão aparece, a estratégia inicia observadores para cada nível de stop configurado. Um observador armazena o preço de origem e espera até que o preço suba ou desça no limite.
4. **Atualização de probabilidade** – Depois que um observador é concluído, os movimentos ascendentes aumentam a estatística de “crescimento”, os movimentos descendentes aumentam a estatística de “declínio”. Um fator de esquecimento emula a lógica de decaimento (`forg`) do especialista original.
5. **Persistência na memória** – Todas as estatísticas são mantidas na memória e redefinidas no início da estratégia, correspondendo ao comportamento da versão MQL quando `ReadHistory` está desabilitado.

## Lógica de negociação
1. As probabilidades de continuação são calculadas para o padrão atual em cada escada delta.
2. Um sinal de compra requer:
   - Probabilidade ≥ `ProbabilityThreshold`.
   - Observações ≥ `MinSamples`.
   - O tempo de espera decorreu desde a compra anterior.
   - Se existir uma posição curta, a nova probabilidade deve exceder a probabilidade de venda armazenada mais o `ProbabilityBuffer`.
3. Um sinal de venda reflete as regras de compra com as funções de crescimento/declínio trocadas.
4. As entradas usam `BuyMarket` / `SellMarket` para emular `OrderSend`. Quando a posição oposta está aberta a estratégia a fecha primeiro, reproduzindo o comportamento de reversão do consultor especialista.
5. As saídas de proteção são tratadas internamente: as paradas e as tomadas são expressas em unidades de preço derivadas do valor do ponto e do nível de parada.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Tipo de dados Candle usado para backtest/sessão ao vivo. | Período de 1 minuto |
| `ProbabilityThreshold` | Probabilidade mínima de continuação necessária para abrir uma negociação. | 0,8 |
| `BaseDeltaPoints` | Distância do ponto base que define a primeira escada sintética. | 1 |
| `DeltaSteps` | Número de escadas delta a serem avaliadas. | 20 |
| `PatternLength` | Número de elementos no histórico da escada. | 10 |
| `StopLevels` | Contagem de níveis de stop/take. | 1 |
| `StopDistancePoints` | Parada base/medida de distância em pontos. | 25 |
| `ForgetFactor` | Decaimento aplicado aos contadores de crescimento/declínio após cada observação. | 1.05 |
| `MinSamples` | Número mínimo de observações concluídas. | 10 |
| `ProbabilityBuffer` | Probabilidade extra necessária para fechar a posição oposta. | 0,05 |
| `FixedVolume` | Volume base de comércio. | 1 lote |
| `UseReinvest` | Permite ajuste de volume proporcional ao equilíbrio. | verdade |
| `VolumeMode` | 0 – fixo, 1 – por cento por 10k, 2 – escada, 3 – linear. | 1 |
| `PercentPer10k` | Percentagem por 10 000 unidades no modo 1. | 3 |
| `BaseDeposit` | Depósito base para modos 2 e 3. | 500 |
| `DepositStep` | Incremento de depósito para os modos 2 e 3. | 500 |
| `MaxVolume` | Limite máximo de volume. | 10.000 |
| `CooldownFactor` | Número de intervalos de velas usados como resfriamento de reativação. | 2 |

## Diferenças do especialista MQL
- A persistência baseada em arquivo foi removida; as estatísticas são reconstruídas do zero sempre que a estratégia é iniciada.
- Os pedidos são simulados por meio de `BuyMarket`/`SellMarket` e gerenciamento de parada de software em vez de pedidos pendentes MT4.
- Os auxiliares de dimensionamento de posição foram adaptados aos dados do portfólio StockSharp. Se os valores patrimoniais não estiverem disponíveis, a estratégia volta ao volume fixo.
- As entradas de trailing stop do código original são ignoradas porque a versão MT4 nunca as aplicou.

## Notas de uso
- Anexe a estratégia a um título com um `PriceStep` válido. Se o passo for desconhecido, a estratégia volta para 0,0001.
- O processo de aprendizagem necessita de um número mínimo de ativações na escada; espere uma fase de aquecimento antes do início das negociações.
- Aumentar `DeltaSteps` ou `PatternLength` aumenta o uso de memória exponencialmente porque o dicionário de padrões cresce rapidamente.
- O limite de probabilidade padrão (0,8) é muito rigoroso. Reduza o valor para negociações mais frequentes.
