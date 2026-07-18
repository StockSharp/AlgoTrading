# Estratégia de Trading com RSI Qualificado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o consultor especialista do MetaTrader "Trade on qualified RSI" usando a API de alto nível do StockSharp. Ela se comporta como um sistema contrário: interpreta leituras prolongadas do Índice de Força Relativa (RSI) como exaustão e abre uma posição contra o movimento predominante após o momentum persistir por várias velas. Os stops trailing são gerenciados em passos de preço para que o stop siga a operação apenas quando o preço se move a favor da mesma.

## Lógica de sinal
### Indicador
* Índice de Força Relativa com um período configurável (padrão: 28).
* Calculado sobre a subscrição de velas selecionada (padrão: velas de 15 minutos).

### Entrada curta
1. A última vela fechada tem RSI maior ou igual ao limiar superior (padrão: 55).
2. Cada uma das `CountBars` velas fechadas anteriores também teve RSI acima do mesmo limiar. Internamente a estratégia conta barras consecutivas; o sinal é disparado quando o contador atinge `CountBars + 1`.
3. Não há posição ativa aberta. Quando ativada, a estratégia vende a mercado com o volume configurado e armazena o fechamento da vela como preço de entrada.

### Entrada longa
1. A última vela fechada tem RSI menor ou igual ao limiar inferior (padrão: 45).
2. Cada uma das `CountBars` velas fechadas anteriores também teve RSI abaixo do mesmo limiar (são necessárias `CountBars + 1` leituras consecutivas).
3. Não existe posição aberta. Quando ativada, a estratégia compra a mercado com o volume configurado e registra o preço de entrada.

## Gestão de posição
* **Stop inicial:** logo após a entrada, o preço de stop é colocado a `StopLossPoints` passos de preço do fechamento de entrada (abaixo para comprados, acima para vendidos). Os passos de preço são obtidos de `Security.PriceStep`; se o ativo não o define, a estratégia recorre a `1`.
* **Trailing:** em cada vela terminada o stop é ajustado em direção ao fechamento atual. Para posições compradas o stop torna-se `Fechamento - StopLossPoints * PriceStep` quando esse valor está acima do stop anterior. Para posições vendidas o stop torna-se `Fechamento + StopLossPoints * PriceStep` quando esse valor está abaixo do stop anterior.
* **Saída:** se a mínima da vela cruzar abaixo do stop quando comprado, ou a máxima da vela cruzar acima do stop quando vendido, a estratégia sai de toda a posição a mercado. Não há alvos de lucro adicionais nem sinais de reversão; novas entradas ocorrem apenas depois que a posição anterior for fechada.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | --------- | ------ |
| `RsiPeriod` | Comprimento de retrospecto para o indicador RSI. | 28 |
| `UpperThreshold` | Nível de RSI que qualifica uma configuração vendida. | 55 |
| `LowerThreshold` | Nível de RSI que qualifica uma configuração comprada. | 45 |
| `CountBars` | Quantas barras anteriores devem permanecer além do limiar (`CountBars + 1` barras consecutivas no total). | 5 |
| `StopLossPoints` | Distância do stop expressa em passos de preço. O deslocamento de preço real é igual a `StopLossPoints * PriceStep`. | 21 |
| `TradeVolume` | Volume enviado com cada ordem de entrada. | 1 |
| `CandleType` | Subscrição de velas usada para os cálculos do indicador. | Velas de 15 minutos |

Todos os parâmetros podem ser otimizados. Os limiares permitem valores decimais, portanto é possível um ajuste fino dos limites do RSI.

## Notas de implementação
* A estratégia usa `SubscribeCandles(...).Bind(...)` para alimentar o indicador RSI e reagir apenas quando a vela está completamente formada.
* Os valores do RSI não são lidos de volta do indicador por índice; em vez disso, contadores rastreiam quantas velas terminadas consecutivas respeitam os limiares.
* Os stops protetores são simulados dentro da estratégia. As ordens são fechadas a mercado quando o nível de stop é cruzado em vez de colocar ordens de stop separadas.
* Mensagens de log são produzidas para entradas e saídas, espelhando a saída detalhada do consultor especialista original.

## Uso
1. Adicione a estratégia a uma aplicação StockSharp, atribua o ativo e portfólio desejados e configure a série de velas.
2. Ajuste os limiares do RSI, o número de barras qualificadas e a distância do stop para corresponder à volatilidade do instrumento alvo.
3. Inicie a estratégia. Monitore o log para ver quando os sinais ocorrem e como o stop trailing evolui.
4. Considere executar o otimizador incorporado para buscar melhores combinações de limiares ou distâncias de stop para mercados específicos.
