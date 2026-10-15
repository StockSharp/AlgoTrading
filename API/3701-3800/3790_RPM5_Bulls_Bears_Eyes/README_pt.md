# Estratégia RPM5 BullsBearsEyes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **RPM5 BullsBearsEyes Strategy** é uma versão C# do MetaTrader 4 expert *Rpm5_mt4v1*. O consultor reconstruiu o oscilador BullsBearsEyes personalizado a partir das leituras Bulls Power e Bears Power e abriu uma única posição que seguiu a tendência predominante. Esta versão StockSharp reproduz o mesmo comportamento usando o API de alto nível, mantendo os parâmetros de risco originais, lógica de rastreamento e limites de sinal.

## Reconstrução do indicador
- Dois osciladores clássicos – **Bulls Power** e **Bears Power** – são calculados na série de velas configurada.
- Sua soma é passada através do suavizador IIR de quatro estágios idêntico usado pelo indicador MT4. O fator de suavização (`Gamma`) controla a rapidez com que o oscilador reage.
- A saída filtrada é transformada em um valor entre **0** e **1**. Valores acima do limite central sinalizam dominância de alta, valores abaixo dele apontam para controle de baixa. Zero ou um exato aparecem quando um dos lados está completamente esgotado, correspondendo aos casos extremos do indicador original.

## Regras de negociação
1. A estratégia assina o período de tempo selecionado (5 minutos por padrão) e aguarda apenas as velas concluídas.
2. Quando plana, avalia a proporção BullsBearsEyes:
   - **Entrada longa** – valor atual estritamente acima de `Threshold` (padrão 0,5).
   - **Entrada curta** – valor atual estritamente abaixo de `Threshold`.
   - O algoritmo mantém no máximo uma posição aberta. Os sinais opostos são ignorados até que a posição ativa seja totalmente fechada pela gestão de risco.
3. Uma vez em uma negociação, a posição permanece intacta até que ocorra um evento de stop-loss, take-profit ou trailing stop.

## Gestão de risco
- **As distâncias de stop-loss/take-profit** são recriadas a partir das configurações originais de 25/150 pip. Eles são recalculados usando o instrumento `PriceStep` (pip) cada vez que uma nova posição é aberta.
- **ATR trailing**: em cada vela concluída, o intervalo médio verdadeiro (período `AtrPeriod`, padrão 5) é avaliado. A distância final é igual a um pip mais `AtrMultiplier × ATR`. Quando o fechamento avança além dessa distância, o stop de proteção é apertado para manter a lacuna, idêntico à lógica MQL que repetidamente chamava `OrderModify`.
- Os níveis de proteção são verificados antes de processar novos sinais, garantindo que as saídas sejam sempre priorizadas em relação às novas entradas, assim como na fonte EA.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Bulls/Bears Period` | 13 | Período médio para os indicadores Bulls Power e Bears Power. |
| `Gamma` | 0,5 | Razão de suavização IIR de quatro estágios para o oscilador BullsBearsEyes. |
| `Threshold` | 0,5 | Divisor entre zonas de alta (> limite) e de baixa (< limite). |
| `ATR Period` | 5 | Lookback usado para o trailing stop baseado em ATR. |
| `ATR Multiplier` | 1,5 | Multiplicador aplicado a ATR ao derivar a distância final. |
| `Stop Loss (pips)` | 25 | Distância de parada protetora, convertida de pips em preço. |
| `Take Profit (pips)` | 150 | Distância alvo de lucro, convertida de pips em preço. |
| `Trade Volume` | 1 | Volume de ordens de mercado utilizado para cada nova posição. |
| `Candle Type` | Velas de 5 minutos | Prazo processado pela estratégia. |

## Notas
- O port não desenha os objetos visuais do canal diário que estavam presentes no MT4 porque eram apenas cosméticos.
- Todos os comentários dentro do código são escritos em inglês conforme solicitado.
- Os testes permanecem inalterados; execute as verificações de nível de solução existentes se a validação for necessária.
