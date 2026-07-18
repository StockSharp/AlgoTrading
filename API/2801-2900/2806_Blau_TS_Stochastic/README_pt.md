# Estratégia Blau TS Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port para StockSharp do expert advisor do MetaTrader "Exp_BlauTSStochastic". O sistema opera com o oscilador estocástico de triplo suavizado de William Blau que estava incluído no pacote MQL original. O indicador calcula os preços máximos e mínimos ao longo de uma janela de retrocesso configurável, suaviza o numerador e denominador estocástico três vezes com a família de média móvel selecionada, redimensiona o resultado para o intervalo [-100, 100], e finalmente produz uma linha de sinal suavizada. Todos os cálculos são realizados em candles terminados que são entregues através da API de subscrição de candles de alto nível.

O indicador pode ser construído a partir de qualquer um dos preços aplicados suportados (fechamento, abertura, máxima, mínima, mediana, típico, ponderado, simples, quartil, duas variantes de seguimento de tendência, ou DeMark) e quatro algoritmos de suavização diferentes (SMA, EMA, SMMA/RMA, WMA). A configuração `SignalBar` permite reproduzir o deslocamento de barra usado pelo expert advisor original: a estratégia avalia sinais em dados com `SignalBar` barras de idade, portanto com o valor padrão de 1 reage à barra que acabou de fechar no passo anterior.

## Regras de entrada e saída

Três modos de trading estão disponíveis. Em cada modo, os interruptores booleanos `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit` e `EnableShortExit` controlam se as respectivas ações são permitidas.

### Modo Breakdown

*Entrada comprada*: o valor anterior do histograma (deslocamento `SignalBar+1`) está acima de zero e o valor mais recente (deslocamento `SignalBar`) está em ou abaixo de zero. Isso reflete a condição original de "histograma rompe a zero" e abre ou inverte uma posição comprada enquanto também cobre quaisquer vendidos.

*Entrada vendida*: o valor anterior do histograma está abaixo de zero e o valor mais recente está em ou acima de zero, sinalizando uma ruptura da linha zero na direção oposta. A estratégia abre ou inverte para uma posição vendida e opcionalmente fecha a exposição comprada.

As mesmas condições também acionam saídas no lado oposto: quando o histograma passa a barra anterior acima de zero, a estratégia fecha vendidos, e quando passa a barra anterior abaixo de zero, fecha comprados.

### Modo Twist

*Entrada comprada*: o histograma forma um fundo local. Concretamente, o valor no deslocamento `SignalBar+1` está abaixo do valor no deslocamento `SignalBar+2`, mas o valor no deslocamento `SignalBar` vira para cima e excede a barra intermediária. Isso reproduz o modo de "mudança de direção" do expert advisor.

*Entrada vendida*: o histograma forma um topo local. O valor no deslocamento `SignalBar+1` é maior que o valor no deslocamento `SignalBar+2`, e o valor mais recente cai abaixo da barra intermediária. Posições na direção oposta são fechadas quando um twist ocorre contra elas.

### Modo CloudTwist

Este modo segue as mudanças de cor da nuvem do indicador que é definida pelo histograma e sua linha de sinal.

*Entrada comprada*: o histograma estava acima da linha de sinal na barra anterior, mas o valor mais recente cruzou abaixo ou tocou a linha de sinal. A estratégia trata a mudança de cor da nuvem como um sinal de alta e opcionalmente cobre vendidos.

*Entrada vendida*: o histograma estava abaixo da linha de sinal na barra anterior, mas o valor mais recente cruzou acima ou tocou a linha de sinal. Isso inverte para uma posição vendida e opcionalmente sai de comprados.

## Gestão de risco

* `StopLossPoints` e `TakeProfitPoints` são medidos em passos de preço do instrumento. Se qualquer valor for maior que zero, a estratégia habilita o bloco de proteção interno do StockSharp com ordens de mercado, para que os stops sigam a posição ativa automaticamente.
* O tamanho da ordem é retirado da propriedade `Volume` da estratégia. Quando um sinal de reversão aparece, a estratégia envia `Volume + |Position|` contratos, garantindo que a posição existente seja fechada antes de abrir uma nova.

## Parâmetros

* `CandleType` – período (tipo de dados) usado para o oscilador (padrão: candles de 4 horas).
* `Mode` – algoritmo de detecção de sinal: `Breakdown`, `Twist` ou `CloudTwist`.
* `AppliedPrice` – fonte de preço para o cálculo estocástico (fechamento, abertura, máxima, mínima, mediana, típico, ponderado, simples, quartil, seguimento de tendência 0/1, ou DeMark).
* `Smoothing` – família de média móvel usada para todos os estágios de suavização (`Simple`, `Exponential`, `Smoothed`, `Weighted`).
* `BaseLength` – número de barras usadas para calcular o intervalo máximo/mínimo.
* `SmoothLength1`, `SmoothLength2`, `SmoothLength3` – comprimentos de suavização para o numerador e denominador (aplicados sequencialmente).
* `SignalLength` – comprimento de suavização para a linha de sinal do histograma.
* `SignalBar` – deslocamento de barra que define quais valores históricos são usados para decisões.
* `StopLossPoints`, `TakeProfitPoints` – tamanho do stop protetor e alvo em passos de preço (0 desabilita a ordem correspondente).
* `EnableLongEntry`, `EnableShortEntry`, `EnableLongExit`, `EnableShortExit` – interruptores de permissão para as quatro ações básicas.

Defina o `Volume` desejado, anexe a estratégia a um instrumento e inicie-a. Todos os cálculos dependem de candles terminados, portanto o sistema aguarda até que os indicadores estejam formados antes de permitir trades.
