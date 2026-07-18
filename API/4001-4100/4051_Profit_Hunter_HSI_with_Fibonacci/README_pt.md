# Profit Hunter HSI com estratégia Fibonacci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma versão C# do MetaTrader 4 consultor especialista `Profit_Hunter_HSI_with_fibonacci.mq4`. O roteiro original combina
um filtro de média móvel exponencial intradiária (EMA) com zonas de retração Fibonacci derivadas do gráfico diário. O StockSharp
a implementação segue a mesma ideia usando o API de alto nível: ela assina dois fluxos de velas (intradiário e diário), calcula
a grade Fibonacci dinamicamente, gera sinais de negociação quando o preço interage com essas bandas e gerencia a posição resultante
com posicionamento de parada adaptável e uma lógica de trailing stop escalonada.

## Fluxo de dados de mercado
1. **Intraday candles** – the `TimeFrame` parameter defines the working resolution (default: 1 minute). Cada vela acabada alimenta
o filtro de tendência EMA, atualiza a referência de suporte/resistência mais recente obtida há `NumBars` barras e aciona a negociação
lógica.
2. **Velas diárias** – uma assinatura dedicada coleta dados de períodos de tempo mais longos. Dois índices configuráveis pelo usuário escolhem a oscilação mais alta
e swing baixo usado como âncoras para a grade Fibonacci. Sempre que uma nova vela diária chega, toda a escada de retração é
recalculado, incluindo as prorrogações (161,8%, 261,8%, 423,6%).

## Geração de Sinal
O consultor MQL armazenou o último balanço alto/baixo descoberto e determinou qual deles aconteceu primeiro (`highFirst`). O porto mantém o
mesmo conceito comparando os índices diários:
- Se a máxima selecionada for mais recente que a mínima selecionada (`highFirst = true`) o mercado é tratado como descendente e o
Os níveis de Fibonacci são medidos de baixo para cima.
- Caso contrário, o movimento é considerado ascendente e a grade é projetada para baixo a partir do topo.

For every completed intraday candle the following rules mirror the original EA:
1. **Filtro de tendência** – um EMA com período `MaPeriod` classifica o viés de curto prazo. Se o preço de fechamento (tratado como oferta e venda)
está acima de EMA a tendência é "Naik" (para cima); se estiver abaixo, a tendência é “Turun” (para baixo). Quando o preço oscila exatamente em torno do
EMA nenhuma negociação será aberta.
2. **Fibonacci sinal** – dependendo de `highFirst` a interação do preço com os níveis de 23,6%, 76,4%, 91% e 14,6% produz um de
quatro sinais de string do código MT4: `Reverse-Buy`, `Reverse-Sell`, `Trading-Area` ou `Continuation`. Only the first three are
usado para entradas reais, o último simplesmente relata uma continuação da tendência.
3. **Regras de entrada** – o script original continha seis ramificações de entrada. Eles são reproduzidos literalmente:
   - Tendência de alta + área de negociação + rompimento acima da resistência de referência → compre com stop de proteção no suporte de referência.
   - Tendência de alta + venda reversa + `highFirst == false` + preço ainda abaixo da resistência → abrir uma venda com stop no nível de 14,6%.
   - Up trend + reverse buy + `highFirst == false` + price below resistance → buy with the stop at the 91% level.
   - Tendência de baixa + área de negociação + quebra sob suporte → venda com parada na linha de resistência.
   - Down trend + reverse sell + `highFirst == true` + price below resistance → sell with the stop at the 91% level.
   - Tendência de baixa + compra reversa + `highFirst == true` + preço abaixo da resistência → compra com stop no nível de 14,6%.
Apenas uma posição pode existir por vez; pedidos ativos não são empilhados.

## Gerenciamento de posição
- **Saídas de suporte/resistência** – como no EA, uma posição longa é liquidada se o preço cair de volta para a referência de suporte enquanto um
A posição curta é fechada quando o preço sobe para a referência de resistência, independentemente do lucro atual.
- **Parada protetora inicial** – o nível de parada calculado durante a decisão de entrada é armazenado internamente e usado como gatilho de saída.
A versão StockSharp realiza a mesma verificação em cada vela em vez de modificar diretamente as ordens da corretora.
- **Stepped trailing stop** – the MQL script raised the stop level every 20 points after an initial 60-point move (e.g., +60 → stop
até +55, +80 → pare até +75, … até +260). A porta mantém a escada exata usando o instrumento `PriceStep` para converter pontos em
price offsets. Para negociações curtas, o stop desliza para baixo para travar os lucros, garantindo a mesma distância do original.

## Parâmetros
| Nome | Descrição | Padrão | Notas |
| --- | --- | --- | --- |
| `NumBars` | Mudança da vela cuja máxima/mínima se torna a resistência/suporte temporário. | `3` | Corresponde à entrada externa `numBars`; deve ser maior que zero. |
| `MaPeriod` | Período do EMA usado para classificação de tendências. | `5` | Equivalente a `maPeriod` no EA. |
| `TimeFrame` | Prazo da vela intradiária. | `1 minute` | Espelha o externo `timeFrame`; accepts any `TimeSpan`. |
| `DaysBackForHigh` | Índice da vela diária que fornece a oscilação máxima. | `1` | Corresponde a `daysBackForHigh`. |
| `DaysBackForLow` | Índice da vela diária que fornece a oscilação mínima. | `1` | Corresponde a `daysBackForLow`. |
| `Volume` | Market order size. | `1` | Represents lots/shares; validado para permanecer positivo. |

## Notas de implementação
- O EA original criou vários objetos gráficos. Those calls are intentionally omitted because StockSharp handles charting
separadamente e as formas eram puramente cosméticas.
- Em vez de consultar buffers históricos como `iLow` e `iHigh`, a porta mantém duas listas na memória de velas concluídas e
reads the required shift directly from there.
- O gerenciamento de parada é implementado no código de estratégia (`ManagePosition`) e não via `OrderModify`, o que mantém o corretor de comportamento
agnóstico, preservando a mesma árvore de decisão.
- As rejeições de pedidos limpam o estado de entrada pendente para que os ajustes manuais não deixem sinalizadores internos obsoletos, correspondendo ao estado defensivo
codificação presente em muitas estratégias API existentes.

## Diferenças da versão MetaTrader
- MetaTrader assumiu acesso ao nível de tick `Ask` e `Bid`. StockSharp opera no fechamento de velas por padrão; the close price is used
como proxy de oferta e solicitação, o que é suficiente para replicar a lógica de decisão.
- The notion of "which extremum appeared first" cannot rely on MT4's `High[]`/`Low[]` series. A porta se aproxima comparando
the selected day indices, delivering identical results for the default configuration and preserving the intended behaviour for
other settings.
- Broker-side stop and take-profit orders are replaced with virtual exits evaluated per candle. Isso evita ordem específica do conector
types while ensuring the same exit conditions are met.
