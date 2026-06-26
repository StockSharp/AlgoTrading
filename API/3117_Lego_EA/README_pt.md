# Estratégia Lego EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia Lego EA** é um porte direto do assessor especialista MetaTrader "Lego EA". Usa uma combinação configurável de filtros técnicos—Commodity Channel Index, médias móveis duplas, oscilador estocástico, Accelerator Oscillator, DeMarker e Awesome Oscillator—para validar entradas e saídas. Cada filtro pode ser ativado ou desativado independentemente para entradas e saídas, permitindo reconstruir o "Lego" original bloco por bloco ou experimentar configurações personalizadas.

## Parâmetros
- `Volume` – volume de trading base usado quando a operação anterior foi lucrativa.
- `LotMultiplier` – multiplicador aplicado ao último volume executado após uma operação perdedora (recuperação estilo martingale).
- `StopLossPips` – stop de proteção expresso em pips (convertido internamente usando o tamanho de tick do símbolo).
- `TakeProfitPips` – alvo de lucro em pips.
- `UseCciForEntry` / `UseCciForExit` – ativar o filtro CCI ao abrir ou fechar posições.
- `UseMaForEntry` / `UseMaForExit` – usar o cruzamento de médias móveis rápida/lenta para confirmações.
- `UseStochasticForEntry` / `UseStochasticForExit` – exigir alinhamento do estocástico %K/%D dentro dos limites configurados.
- `UseAcceleratorForEntry` / `UseAcceleratorForExit` – exigir padrões de aceleração do Accelerator Oscillator.
- `UseDemarkerForEntry` / `UseDemarkerForExit` – aplicar verificações de nível DeMarker.
- `UseAwesomeForEntry` / `UseAwesomeForExit` – incluir confirmação de momentum do Awesome Oscillator.
- `CciPeriod` – período do Commodity Channel Index.
- `MaFastPeriod` / `MaSlowPeriod` – comprimentos de lookback para as médias móveis rápida e lenta.
- `MaShift` – número de barras completadas para deslocar os valores da média móvel no tempo, reproduzindo o parâmetro de deslocamento horizontal do MT5.
- `MaMethod` – método de suavização (simples, exponencial, suavizado ou ponderado).
- `MaPrice` – fonte de preço da vela fornecida a ambas as médias móveis.
- `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlow` – configuração do oscilador estocástico.
- `StochasticLevelUp` / `StochasticLevelDown` – limites de sobrecomprado/sobrevendido usados para sinais.
- `DemarkerPeriod`, `DemarkerLevelUp`, `DemarkerLevelDown` – configurações do oscilador DeMarker.
- `CandleType` – período da série de velas usada por todos os indicadores.

## Fluxo de trabalho de trading
1. Em cada vela completada, a estratégia coleta valores de indicadores dos filtros selecionados.
2. Cada filtro calcula a prontidão de compra/venda com base na barra anterior completamente formada (correspondendo ao offset `iGetArray(..., 1)` do EA original).
3. Uma entrada comprada é permitida apenas quando **todos os filtros de entrada habilitados** concordam em um sinal altista. Da mesma forma, uma entrada vendida requer confirmação baixista unânime.
4. Se a conta estiver flat e um sinal de entrada válido aparecer, uma ordem a mercado é enviada usando o `Volume` base ou o último volume de operação perdedora multiplicado por `LotMultiplier`.
5. Quando já há uma posição, os filtros de saída habilitados são avaliados da mesma forma. A posição é fechada apenas quando todos os filtros de saída concordam em um sinal oposto.
6. A proteção de stop-loss e take-profit é instalada automaticamente usando `StartProtection`, convertendo entradas em pips para distâncias de preço absolutas com base no tamanho de tick do símbolo.

## Gerenciamento de capital
- Após uma operação vencedora, a próxima ordem reverte para o `Volume` base.
- Após uma operação perdedora, o volume é multiplicado por `LotMultiplier`, emulando a lógica de escalada de lotes do EA original.
- Limites de volume impostos pela bolsa (passo, mín. e máx.) são aplicados antes de cada ordem.

## Notas e diferenças em relação à versão MetaTrader
- As fontes de preço do indicador mapeiam para equivalentes do StockSharp. CCI usa o preço típico internamente e as médias móveis usam a fonte `MaPrice` selecionada.
- Todos os cálculos de indicadores dependem de velas completamente fechadas. Isso evita dados parcialmente formados e imita o processamento de "nova barra" do EA.
- Verificações de nível de freeze e colocação manual de preço de SL/TP são tratadas pelo serviço `StartProtection` do StockSharp.
- Saídas parciais de posição atualizam o estado de rastreamento de perda apenas quando toda a posição está flat, correspondendo à lógica `DEAL_ENTRY_OUT` do EA.

## Dicas de uso
- Comece com a configuração original (filtro MA habilitado, outros filtros desabilitados) para reproduzir o comportamento base, depois habilite filtros adicionais para melhorar a qualidade do sinal.
- Monitore a exposição da conta ao usar valores altos de `LotMultiplier`; o risco cresce rapidamente durante sequências de perdas.
- Combine a estratégia com o Backtester para confirmar se a combinação de filtros escolhida está alinhada com os instrumentos que planeja operar.

Esta estratégia atualmente não tem versão em Python.
