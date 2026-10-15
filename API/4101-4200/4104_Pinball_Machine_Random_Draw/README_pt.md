# Estratégia de sorteio aleatório de máquina de pinball
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão StockSharp direta do MetaTrader 4 consultor especialista `Pinball_machine.mq4`. O robô original desenhou
inteiros aleatórios em cada tick recebido e abria uma ordem de mercado sempre que dois desses sorteios correspondiam. A versão StockSharp
preserva o mesmo comportamento de loteria: em cada vela finalizada do intervalo de tempo selecionado, o algoritmo executa dois pares de
sorteia aleatoriamente e entra em uma posição de mercado longa ou curta quando o par correspondente contém valores iguais. Stop-loss e take-profit
as distâncias também são aleatórias em cada avaliação, reproduzindo a sensação da rotina original de "pinball", onde as negociações saltam e
fora imprevisível.

## Lógica de negociação
- Assine as velas definidas pelo parâmetro `CandleType` e aguarde as barras totalmente formadas.
- Para cada vela finalizada gere quatro inteiros distribuídos uniformemente em `[0, RandomMaxValue]`. O primeiro par pertence ao
potencial entrada longa, o segundo par pertence à potencial entrada curta.
- Desenhe dois inteiros adicionais entre `MinStopLossPoints`/`MaxStopLossPoints` e `MinTakeProfitPoints`/`MaxTakeProfitPoints` para
determinar as distâncias de proteção (expressas em etapas de preços) compartilhadas por ambos os lados da avaliação.
- Se o primeiro e o segundo números inteiros aleatórios corresponderem, envie uma ordem de compra a mercado com volume `TradeVolume`. Se o terceiro e o quarto
valores coincidem, envie uma ordem de venda a mercado com o mesmo volume. Ambas as condições podem disparar dentro da mesma vela, exatamente como em
a versão MQL onde as ordens de compra e venda eram eventos independentes.
- Anexe imediatamente uma ordem de stop-loss e de take-profit (se a distância desenhada for maior que zero). As distâncias são interpretadas
como múltiplos do `PriceStep` do instrumento, espelhando o multiplicador `Point` usado em MetaTrader.

## Gerenciamento de pedidos e controles de risco
- `StartProtection()` é invocado quando a estratégia é iniciada para que StockSharp gerencie ordens de proteção em nome da estratégia.
- Cada entrada mede a posição resultante (`Position ± TradeVolume`) e a passa para `SetStopLoss` e `SetTakeProfit`, que
permite que a plataforma consolide ordens de proteção mesmo quando várias negociações estão em execução ao mesmo tempo.
- Se os parâmetros de distância mínima ou máxima forem definidos como zero ou um número negativo, a proteção correspondente é
pulou para esse ciclo.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradeVolume` | Tamanho do pedido em lotes/contratos enviados para cada entrada aleatória. |
| `CandleType` | Prazo das velas que acionam os sorteios aleatórios. Períodos mais curtos emulam o EA original baseado em ticks mais de perto. |
| `RandomMaxValue` | Limite superior inclusivo para sorteios de inteiros. Um valor maior diminui a probabilidade de correspondência de números e, portanto, reduz a frequência de negociação. |
| `MinStopLossPoints` | Limite inferior (em etapas de preço) para a distância de stop-loss gerada aleatoriamente. |
| `MaxStopLossPoints` | Limite superior (em etapas de preço) para a distância do stop loss. |
| `MinTakeProfitPoints` | Limite inferior (em etapas de preço) para a distância de lucro gerada aleatoriamente. |
| `MaxTakeProfitPoints` | Limite superior (em etapas de preço) para a distância de realização do lucro. |
| `RandomSeed` | Semente do gerador de números pseudo-aleatórios. Zero mantém o comportamento baseado no tempo; qualquer outro valor torna a sequência reproduzível. |

## Notas de implementação
- O script MetaTrader foi orientado por ticks; a porta StockSharp usa conclusões de vela porque o API de alto nível opera em eventos de série temporal. Definir um `CandleType` muito curto (por exemplo, velas de um segundo ou de tick) restaura a natureza acelerada do original.
- Os valores de stop-loss e take-profit são gerados uma vez por avaliação e reutilizados para as ramificações longas e curtas, exatamente como na fonte EA.
- Certifique-se de que o instrumento negociado expõe um `PriceStep` válido, caso contrário, as distâncias de proteção expressas em pontos podem precisar de ajuste manual.
