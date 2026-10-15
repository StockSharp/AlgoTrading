# Estratégia BrainTrend2 + AbsolutelyNoLagLWMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia combina dois módulos independentes que foram originalmente implementados no MetaTrader 5: BrainTrend2_V2 e AbsolutelyNoLagLWMA. Cada módulo analisa sua própria assinatura de candles e decide quando ir comprado, ir vendido ou voltar ao flat. O port em C# mantém ambos os fluxos de decisão intactos e agrega sua exposição desejada em uma única estratégia StockSharp.

* **Módulo BrainTrend2.** Usa um estado de cor de seguimento de tendência gerado pelo indicador BrainTrend2. O estado é derivado de um canal baseado em ATR que muda quando o preço viola o limite oposto.
* **Módulo AbsolutelyNoLagLWMA.** Rastreia a inclinação de uma média móvel ponderada linealmente com dupla suavização calculada em um preço aplicado selecionável.

Quando um dos módulos solicita uma nova direção de posição, a estratégia recalcula o volume alvo combinado e envia ordens de mercado para atingir essa exposição. A configuração padrão opera em candles H4 para ambos os indicadores, mas cada módulo pode assinar um período diferente.

## Indicadores
### BrainTrend2
O indicador BrainTrend2 reconstrói a sobreposição de candles de cinco cores do arquivo MQL original:
* Uma série de amplitude verdadeira ponderada triangularmente (parâmetro de período) é escalonada por um coeficiente de 0.7 para formar uma banda dinâmica (`widcha`).
* Um nível de referência flutuante (`Emaxtra`) segue os extremos de preço dentro do regime atual.
* Quando a mínima cai abaixo de `Emaxtra - widcha`, o regime muda para baixista. Quando a máxima excede `Emaxtra + widcha`, o regime muda para altista.
* O regime resultante determina a cor: lima/teal (valores 0 ou 1) para contextos altistas, marrom/magenta (valores 3 ou 4) para contextos baixistas, cinza (valor 2) antes do indicador estar pronto.

O indicador em C# mantém a mesma mecânica, incluindo a estimativa ATR triangular, para que as cores geradas correspondam à referência MQL.

### AbsolutelyNoLagLWMA
O módulo AbsolutelyNoLagLWMA aplica duas médias móveis ponderadas linearmente consecutivas à série de preços selecionada. A inclinação da linha resultante impulsiona os valores de cor:
* **2 (azul)** – a linha está subindo.
* **1 (cinza)** – a linha está plana.
* **0 (violeta)** – a linha está caindo.

Ambos os indicadores expõem `IsFormed` para que a estratégia aguarde até que histórico suficiente esteja disponível antes de reagir às cores.

## Lógica de Trading
A estratégia mantém dois alvos internos, `_brainTrendTarget` e `_lwmaTarget`, representando o volume desejado para cada módulo. Sempre que um dos módulos muda seu alvo, a estratégia chama `RebalancePosition` para ajustar a posição agregada para `_brainTrendTarget + _lwmaTarget`.

### Módulo BrainTrend2
* Avalia a cor do candle `SignalBar` períodos atrás (padrão 1) e a cor precedente para detectar transições de estado.
* Quando a cor atual é altista (`< 2`) e a cor anterior não era altista (`> 1`), o módulo:
  * Fecha qualquer exposição vendida criada por este módulo.
  * Abre uma posição comprada com `BrainTrendVolume` se as entradas compradas estiverem habilitadas.
* Quando a cor atual é baixista (`> 2`) e a cor anterior não era baixista (`< 3`), o módulo:
  * Fecha qualquer exposição comprada pendente.
  * Abre uma posição vendida com `BrainTrendVolume` se as entradas vendidas estiverem habilitadas.

### Módulo AbsolutelyNoLagLWMA
* Usa a mesma lógica `SignalBar` mas reage aos valores de cor 2 (cima) e 0 (baixo).
* Quando a cor se torna **2** e a cor anterior era diferente:
  * Fechamento opcional da exposição vendida (`LwmaCloseShortAllowed`).
  * Abertura opcional de uma posição comprada com `LwmaVolume` se `LwmaBuyAllowed` for verdadeiro.
* Quando a cor se torna **0** e a cor anterior era diferente:
  * Fechamento opcional da exposição comprada (`LwmaCloseLongAllowed`).
  * Abertura opcional de uma posição vendida com `LwmaVolume` se `LwmaSellAllowed` for verdadeiro.

Cada módulo modifica apenas seu próprio volume alvo, portanto ambos podem estar ativos ao mesmo tempo. Por exemplo, o módulo BrainTrend2 pode ficar comprado enquanto o módulo LWMA realiza scalps vendidos em torno da posição central.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `BrainTrendAtrPeriod` | Período do ATR triangular usado pelo BrainTrend2. |
| `BrainTrendSignalBar` | Número de candles finalizados usados para deslocar sinais BrainTrend2. `1` significa que a estratégia aguarda o fechamento da barra anterior. |
| `BrainTrendBuyAllowed` / `BrainTrendSellAllowed` | Habilitar ou desabilitar entradas compradas/vendidas para o módulo BrainTrend2. |
| `BrainTrendVolume` | Volume colocado pelo módulo BrainTrend2 ao entrar em uma posição. |
| `BrainTrendCandleType` | Tipo de candle (período) assinado pelo módulo BrainTrend2. |
| `LwmaLength` | Comprimento de cada média ponderada no indicador AbsolutelyNoLagLWMA. |
| `LwmaSignalBar` | Deslocamento de sinal para o módulo LWMA (mesma semântica que o módulo BrainTrend). |
| `LwmaAppliedPrice` | Preço aplicado usado para construir o LWMA (fechamento, abertura, mediana, Demark, etc.). |
| `LwmaBuyAllowed` / `LwmaSellAllowed` | Habilitar ou desabilitar entradas compradas/vendidas para o módulo LWMA. |
| `LwmaCloseLongAllowed` / `LwmaCloseShortAllowed` | Permitir que o módulo LWMA feche exposição oposta quando um sinal se inverte. |
| `LwmaVolume` | Volume enviado pelo módulo LWMA quando abre um trade. |
| `LwmaCandleType` | Tipo de candle (período) assinado pelo módulo LWMA. |

## Gestão de Posição e Ordens
* A estratégia sempre usa ordens de mercado (`BuyMarket` / `SellMarket`) para atingir o volume alvo agregado.
* Volumes de ambos os módulos são aditivos. Por exemplo, se cada módulo solicitar `1` lote em direções opostas, a posição líquida se torna zero, efetivamente protegendo a conta.
* Nenhum stop-loss ou take-profit automático é recriado do Consultor Especialista original porque essas funções eram específicas do corretor em MQL. O controle de risco pode ser adicionado via proteções StockSharp se necessário.
* Quando ambos os módulos assinam períodos diferentes, a estratégia assina automaticamente ambos os fluxos de candles e os desenha na área do gráfico junto com os fills.

## Notas
* A implementação mantém os cálculos de indicadores autocontidos, portanto nenhuma biblioteca de indicadores externa é necessária.
* `SignalBar = 0` permite reagir ao candle mais recentemente concluído imediatamente, enquanto deslocamentos maiores impõem confirmação adicional.
* BrainTrend2 requer pelo menos `AtrPeriod + 2` candles históricos antes de emitir cores válidas; AbsolutelyNoLagLWMA precisa de pelo menos `Length` candles.
* Como ambos os módulos compartilham o mesmo `Strategy.Security`, seus trades são reconciliados através da mesma conexão de portfólio, assim como no Consultor Especialista MT5 original que usava números mágicos diferentes.

## Estendendo a Estratégia
* Adicionar proteções de risco StockSharp (ex.: trailing stops) se stops fixos da versão MQL forem necessários.
* Ajustar `BrainTrendVolume` e `LwmaVolume` independentemente para enfatizar o comportamento de seguimento de tendência ou de reversão à média.
* Combinar os módulos com filtros adicionais observando os valores de indicadores fornecidos dentro de `ProcessBrainTrend` e `ProcessLwma`.
