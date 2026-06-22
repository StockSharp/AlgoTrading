# Estratégia de Reversão de Intervalo Pipso
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port StockSharp do consultor especialista Pipso do MQL5. Atua como um sistema de reversão à média que vende em rompimentos de alta e compra em rompimentos de baixa de um intervalo recente de máximo/mínimo, limitando a atividade a uma sessão de negociação configurável.

## Ideia central
- Construir um canal no estilo Donchian a partir da máxima mais alta e da mínima mais baixa dos últimos `LookbackPeriod` candles finalizados (padrão 36).
- Monitorar o limite superior para desaparecer rompimentos para cima e o limite inferior para desaparecer rompimentos para baixo.
- Abrir posições apenas quando o candle atual começa dentro da janela de negociação definida por `StartHour` e `EndHour`.

## Lógica de trading
### Critérios de entrada
- **Entrada vendida**: quando a máxima do candle toca ou supera a máxima do canal anterior, fechar qualquer posição comprada e, se dentro da janela de sessão, vender `OrderVolume` contratos a mercado. O modelo registra o preço de entrada como a máxima do canal.
- **Entrada comprada**: quando a mínima do candle toca ou quebra abaixo da mínima do canal anterior, fechar qualquer posição vendida e, se o trading for permitido, comprar `OrderVolume` contratos a mercado com a mínima do canal como referência de entrada.

### Critérios de saída
- As posições são fechadas imediatamente quando o preço toca o lado oposto do canal (espelhando o comportamento do EA original).
- Um stop de proteção é colocado a uma distância fixa do preço de entrada. A distância do stop equivale a `(channelHigh - channelLow) * (1 + StopRangePercent / 100)`; com o padrão `StopRangePercent = 300` o stop fica a quatro larguras de canal de distância.
- Os stops são avaliados nos extremos do candle: uma posição comprada fecha se a mínima do candle cair abaixo do stop, e uma vendida se a máxima superar o stop.

### Filtro de sessão
- `StartHour` e `EndHour` são especificados no horário da bolsa. Se `StartHour < EndHour` a estratégia negocia apenas entre essas horas no mesmo dia. Se `StartHour > EndHour` a janela cruza a meia-noite, habilitando sessões noturnas (ex.: 21 → 9).
- Quando a janela está desabilitada (`StartHour == EndHour`) a estratégia permanece sem posições.

## Parâmetros
- **OrderVolume** *(padrão 0.1)* – volume de negociação por ordem.
- **LookbackPeriod** *(padrão 36)* – número de candles usados para calcular o canal.
- **StartHour** *(padrão 21)* – hora (0–23) em que a sessão abre.
- **EndHour** *(padrão 9)* – hora (0–23) em que a sessão fecha.
- **StopRangePercent** *(padrão 300)* – percentual adicional da largura do canal adicionado ao intervalo bruto antes de converter em distância de stop.
- **CandleType** *(padrão candles de 1 hora)* – período usado para cálculos.

## Indicadores e dados
- Usa os indicadores `Highest` e `Lowest` do StockSharp para rastrear os limites do canal.
- Funciona com qualquer instrumento que forneça dados de candles contínuos correspondentes ao `CandleType` selecionado.
- O EA original espera que o período do gráfico represente o horizonte de decisão; você pode ajustar `CandleType` para reproduzir essas condições.

## Notas
- A lógica opera em candles finalizados para evitar ruído intrabarra; em feeds ao vivo os preços de stop/entrada aproximam onde o EA MQL5 interagiria com os ticks.
- Nenhum alvo de take-profit é definido — os lucros são realizados quando o preço reverte ao limite oposto ou quando o stop é atingido.
- Considere calibrar as horas de sessão, comprimento do intervalo e multiplicador de stop à volatilidade do instrumento de negociação.
