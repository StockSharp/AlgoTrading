# Estratégia de Troca de Intervalo XD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de Troca de Intervalo XD recria o expert advisor do MetaTrader 5 **Exp_XD-RangeSwitch** usando a API de alto nível do StockSharp. Ela se baseia no indicador de canal personalizado XD-RangeSwitch, que traça bandas superiores e inferiores alternadas junto com setas sempre que a banda dominante muda. A estratégia pode tanto desvanecer essas setas (comportamento contra-tendência) quanto operar na direção do rompimento dependendo do parâmetro `TradeDirection`. O dimensionamento de ordens segue a configuração base `Strategy.Volume`, enquanto as fórmulas originais de gestão de dinheiro são substituídas pelos helpers de gestão de posição do StockSharp.

## Recriação do indicador XD-RangeSwitch
* O indicador rastreia as últimas `Peaks` velas completas para determinar os intervalos de máximas e mínimas mais altos.
* Um canal de alta (banda inferior) é impresso quando o fechamento atual está acima da máxima mais alta das `Peaks` barras anteriores. Seu valor equivale à mínima mais baixa na mesma janela mais a barra atual.
* Um canal de baixa (banda superior) é impresso quando o fechamento atual está abaixo da mínima mais baixa das `Peaks` barras anteriores. Seu valor equivale à máxima mais alta na mesma janela mais a barra atual.
* Se nenhum rompimento ocorrer, os valores anteriores do canal são propagados adiante.
* Sempre que um canal reaparece depois de estar vazio, a estratégia registra um sinal de seta no preço do canal. Isso reflete o comportamento dos buffers 2 e 3 do MT5 usados pelo expert original.
* Apenas velas completamente terminadas são processadas, garantindo valores consistentes em execuções ao vivo e históricas.

## Lógica de trading
1. A estratégia processa velas do período selecionado por `CandleType` e armazena os buffers de indicadores reconstruídos.
2. Para cada nova vela, ela inspeciona o valor do indicador que tem `SignalBar` velas de antiguidade (o código MT5 usa o mesmo deslocamento ao chamar `CopyBuffer`).
3. O mapeamento de sinais depende da opção `TradeDirection`:
   * **AgainstSignal** replica o comportamento padrão do MT5 — setas de alta ativam posições compradas e também solicitam fechar trades vendidos; setas de baixa ativam posições vendidas e solicitam fechar as compradas.
   * **WithSignal** inverte a interpretação, de modo que setas de alta são tratadas como pontos de saída para posições compradas e pontos de entrada para vendidas, operando efetivamente na mesma direção que o rompimento do canal.
4. Buffers de tendência sem setas ainda são respeitados como sinais de saída, correspondendo aos indicadores originais `SELL_Close` e `BUY_Close`.
5. Fechamentos sempre se executam antes das aberturas, permitindo que a estratégia zere uma posição oposta antes de entrar na nova direção.
6. As ordens são enviadas com helpers de execução a mercado (`BuyMarket`/`SellMarket`). Quando uma mudança ocorre enquanto uma posição oposta está aberta, a quantidade solicitada é automaticamente aumentada para compensar completamente a exposição antes de estabelecer a nova posição.

## Gestão de risco
* A lógica opcional de stop-loss e take-profit é fornecida através dos parâmetros `UseStopLoss`/`StopLossPoints` e `UseTakeProfit`/`TakeProfitPoints`.
* As distâncias são medidas em unidades de preço absolutas, refletindo as entradas de "pontos" no script MT5.
* Stops e alvos são avaliados a cada vela terminada usando os valores máximo/mínimo da vela para emular a ativação dentro da barra.
* Se tanto um stop quanto um alvo estiverem ativos, o stop tem prioridade — a posição é fechada assim que qualquer nível for atingido.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Velas H4 | Período utilizado para os cálculos do XD-RangeSwitch. |
| `Peaks` | 4 | Número de picos (comprimento de lookback) analisados pelo indicador. |
| `SignalBar` | 1 | Número de barras completas para trás ao ler os buffers do indicador. |
| `TradeDirection` | AgainstSignal | Escolher entre interpretação contra-tendência ou seguimento de tendência dos sinais. |
| `AllowBuyEntry` / `AllowSellEntry` | true | Habilitar ou desabilitar a abertura de novas posições na direção correspondente. |
| `AllowBuyExit` / `AllowSellExit` | true | Permitir que a estratégia feche posições existentes quando o indicador solicita. |
| `UseStopLoss` / `StopLossPoints` | true / 1000 | Ativar o tratamento de stop-loss e definir sua distância em unidades de preço. |
| `UseTakeProfit` / `TakeProfitPoints` | true / 2000 | Ativar o tratamento de take-profit e definir sua distância em unidades de preço. |

## Notas
* Os buffers de máximas/mínimas são mantidos internamente dentro da estratégia em vez de depender de coleções do StockSharp, mantendo-se fiel à implementação do MT5 e aderindo às diretrizes de conversão.
* Os sinais são avaliados apenas em velas terminadas. Se `SignalBar` for maior que zero, a ordem é colocada na próxima vela após a que produziu o sinal, como no expert do MT5.
* Os valores do indicador são mantidos em um histórico rolante curto que se estende um pouco além do maior entre `Peaks` e `SignalBar`, garantindo uso determinístico de memória mesmo durante simulações longas.
* A configuração padrão reflete os padrões do MT5: velas H4, `Peaks = 4`, `SignalBar = 1`, trading contra-tendência e um envelope de risco de 1.000/2.000 pontos.
