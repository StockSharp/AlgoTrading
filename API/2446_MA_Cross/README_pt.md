# Estratégia de Cruzamento de MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o expert advisor "MA Cross" do MetaTrader 5 (arquivo `MA Cross.mq5`) dentro do framework StockSharp. O sistema observa duas médias móveis configuráveis e emite ordens de mercado sempre que a média rápida cruzar a média lenta. A implementação mantém o nível original de flexibilidade ao expor o método da média móvel, o preço aplicado e o deslocamento do indicador para ambas as curvas.

## Lógica da estratégia
1. Assinar um único fluxo de velas definido pelo parâmetro `CandleType`.
2. Calcular as médias móveis rápida e lenta em cada vela completa. Cada média móvel pode usar um de quatro métodos (simples, exponencial, suavizada ou ponderada linearmente) e lê um dos preços aplicados no estilo MetaTrader (fechamento, abertura, máximo, mínimo, mediana, típico ou ponderado).
3. Armazenar os valores mais recentes do indicador levando em conta o deslocamento configurado, de modo que os testes de cruzamento sejam realizados em valores de barras anteriores quando necessário.
4. Detectar um cruzamento de alta quando a média rápida se move de abaixo da média lenta deslocada para acima. Detectar um cruzamento de baixa quando o movimento oposto ocorre.
5. Emitir ordens de mercado apenas após ambos os indicadores estarem completamente formados e a estratégia estar online. Sinais longos fecham qualquer posição curta existente e abrem uma posição comprada de `OrderVolume`. Sinais curtos fecham qualquer posição comprada existente e abrem uma posição vendida do mesmo tamanho.

A estratégia opera estritamente em velas completadas e nunca inspeciona dados inacabados. A lógica de proteção é ativada através de `StartProtection()` para garantir que o StockSharp monitore a posição aberta em busca de condições anormais.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `FastPeriod` | 3 | Período da média móvel rápida. |
| `SlowPeriod` | 13 | Período da média móvel lenta. |
| `FastMethod` | Simple | Método de média móvel para a linha rápida (simples, exponencial, suavizada ou ponderada linearmente). |
| `SlowMethod` | LinearWeighted | Método de média móvel para a linha lenta. |
| `FastPriceType` | Close | Preço aplicado usado pela linha rápida (fechamento, abertura, máximo, mínimo, mediana, típico, ponderado). |
| `SlowPriceType` | Median | Preço aplicado usado pela linha lenta. |
| `FastShift` | 0 | Número de barras completadas usadas para deslocar a média rápida para a esquerda. |
| `SlowShift` | 0 | Número de barras completadas usadas para deslocar a média lenta para a esquerda. |
| `OrderVolume` | 1 | Volume para cada ordem de mercado. |
| `CandleType` | Período de 1 minuto | Série de dados de velas processada pela estratégia. |

Todos os parâmetros podem ser otimizados dentro do StockSharp porque o construtor os registra usando os helpers `StrategyParam`.

## Regras de trading
- **Entrada comprada:** Ativada quando a média rápida cruza acima da média lenta de acordo com os valores ajustados pelo deslocamento. Se a estratégia já está vendida, ela submete uma única ordem de compra a mercado para fechar a exposição vendida e abrir uma nova posição comprada. Se não há posição, compra exatamente `OrderVolume`.
- **Entrada vendida:** Ativada quando a média rápida cruza abaixo da média lenta. A exposição comprada existente é revertida via uma única ordem de venda a mercado; caso contrário, a estratégia abre um novo trade vendido de `OrderVolume`.
- **Sem escalonamento adicional:** Uma vez posicionado, sinais na mesma direção são ignorados até que o cruzamento oposto ocorra.
- **Estilo de execução:** As ordens são enviadas com `BuyMarket` ou `SellMarket`. A estratégia não configura níveis de stop-loss ou take-profit; a gestão de risco pode ser adicionada através de outros módulos do StockSharp, se necessário.

## Notas de conversão
- A criação do indicador espelha as chamadas `iMA` do MetaTrader. A enumeração personalizada `MovingAverageMethods` mapeia `MODE_SMA`, `MODE_EMA`, `MODE_SMMA` e `MODE_LWMA` para `SimpleMovingAverage`, `ExponentialMovingAverage`, `SmoothedMovingAverage` e `WeightedMovingAverage` do StockSharp, respectivamente.
- O tratamento do preço aplicado reproduz as opções `ENUM_APPLIED_PRICE` do MetaTrader calculando os preços mediana, típico e ponderado diretamente a partir dos dados das velas.
- Os parâmetros de deslocamento reutilizam a lógica original: a estratégia armazena em buffer os valores do indicador e recupera as comparações de entrada e saída de barras anteriores quando `FastShift` ou `SlowShift` são positivos.
- A lógica de gestão de posições corresponde à abordagem original onde sinais opostos primeiro fecham a posição existente e depois estabelecem uma posição na nova direção na mesma barra.
