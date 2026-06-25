# Estratégia de Abertura de Tempo para Duas Sessões
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Abertura de Tempo para Duas Sessões** automatiza um plano de trading agendado por tempo que pode gerenciar duas sessões independentes durante o dia de trading. Cada sessão pode ser configurada com sua própria direção, parâmetros de risco e janela opcional de fechamento forçado. A conversão segue a lógica original do MetaTrader, mas depende das APIs de alto nível do StockSharp, velas e objetos de parâmetros para configuração e otimização.

## Lógica de trading
1. **Janelas de fechamento de sessão.** Para cada intervalo, uma janela de fechamento opcional pode ser definida. Quando o tempo da vela cai dentro da janela (tempo de início mais a duração global), a estratégia fecha forçosamente o intervalo correspondente e limpa seu estado.
2. **Manutenção do trailing stop.** Se o trailing stop e o passo forem positivos, a lógica de trailing monitora as velas concluídas. Uma vez que o preço se move a favor da posição em pelo menos `(TrailingStop + TrailingStep)`, o stop avança em `TrailingStop`. As atualizações requerem a distância do passo para evitar recálculos ruidosos.
3. **Verificações de stop loss e take profit.** Cada intervalo tem distâncias independentes de stop loss e take profit medidas em pips. Em cada vela concluída, os preços máximos/mínimos são comparados com esses níveis, fechando o intervalo imediatamente quando um nível é superado.
4. **Filtro por dia da semana.** O trading prossegue apenas nos dias da semana habilitados. Se a vela atual pertencer a um dia desabilitado, nenhuma nova operação é aberta.
5. **Janelas de abertura.** Cada intervalo tem uma janela de abertura com tempos de início e fim. O valor de duração global estende a janela pelo lado de finalização. Quando uma janela está ativa e o intervalo não tem posição aberta, a estratégia abre uma ordem de mercado na direção configurada.
6. **Sincronização de posição.** Os intervalos ativos contribuem para uma posição líquida alvo. A estratégia chama `BuyMarket` ou `SellMarket` para que a posição líquida corresponda à soma das exposições dos intervalos. Cada intervalo mantém seu próprio preço de entrada, níveis de stop/take e estado do trailing stop.

## Referência de parâmetros
- **Close Window #1 / Close Window #2** – habilitar ou desabilitar as janelas de fechamento forçado dedicadas para cada intervalo.
- **Close Start #1 / Close Start #2** – hora local do dia em que a janela de fechamento começa para cada intervalo.
- **Trailing Stop / Trailing Step** – distâncias em pips usadas pela lógica de trailing. Ambas devem ser maiores que zero para ativar o trailing.
- **Trade Monday … Trade Friday** – filtros por dia da semana. Pelo menos um dia deve permanecer habilitado para permitir o trading.
- **Open Start #1 / Open End #1 / Open Start #2 / Open End #2** – limites de janela de abertura para cada intervalo. A duração global estende a janela além do tempo de fim.
- **Window Duration** – intervalo de tempo extra adicionado às janelas de abertura e fechamento.
- **Direction #1 / Direction #2** – sinalizadores de direção de trade (`true` para comprado, `false` para vendido) para cada intervalo.
- **Trade Volume** – volume de ordem de mercado para cada intervalo. A estratégia assume volume idêntico para ambos os intervalos como no expert advisor original.
- **Stop Loss #1 / Take Profit #1 / Stop Loss #2 / Take Profit #2** – distâncias em pips para os níveis de stop loss e take profit por intervalo. Um valor de zero desabilita o nível correspondente.
- **Candle Type** – série de velas usada para impulsionar a estratégia. Todos os cálculos, incluindo janelas de tempo e verificações de risco, são executados quando essas velas terminam.

## Detalhes de gestão de risco
- As distâncias em pips são convertidas em unidades de preço usando o passo de preço do instrumento. Se o instrumento usar três ou cinco casas decimais, o passo é multiplicado por dez para replicar a definição de pip do MetaTrader.
- A lógica de trailing é compartilhada por ambos os intervalos, enquanto os valores de stop loss e take profit permanecem independentes.
- Quando o nível de stop ou trailing é ativado, o intervalo redefine seu estado para que possa reabrir dentro da mesma janela se o tempo permitir.

## Limitações e notas
- O StockSharp opera com um modelo de posição líquida. Se o intervalo #1 e #2 estiverem configurados com direções opostas, a posição líquida resultante será nivelada em vez de manter duas operações cobertas abertas simultaneamente. Use um portfólio com capacidade de hedge se o hedge real for necessário.
- As decisões são baseadas na série de velas selecionada. Usar um período de tempo grande pode atrasar as reações em comparação com a implementação baseada em ticks do MetaTrader.
- A estratégia espera que os relógios do exchange e do terminal estejam sincronizados porque as comparações de hora do dia são baseadas em hora local.

## Dicas de uso
- Configure o tipo de vela para corresponder à granularidade temporal usada para o agendamento (por ex., um minuto para controle granular).
- Combine o filtro de dia e as janelas de fechamento para evitar carregar posições durante sessões indesejadas.
- Otimize os parâmetros através dos objetos `StrategyParam` integrados; os campos-chave já têm `SetCanOptimize` habilitado.
