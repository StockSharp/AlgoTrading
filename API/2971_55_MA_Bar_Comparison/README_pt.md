# Estratégia FiftyFiveMaBarComparisonStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia replica o consultor especialista "55 MA" do MetaTrader 5, comparando dois pontos de uma média móvel de 55 períodos e negociando sempre que sua diferença excede um limite configurável. Todos os cálculos são realizados em velas completadas dentro de uma sessão intradiária definida pelo usuário, e a direção da operação pode ser opcionalmente invertida. O algoritmo preserva o comportamento original onde uma posição vendida é aberta sempre que nenhuma condição de alta é atendida.

## Lógica de trading
1. Inscrever-se na série de velas selecionada e calcular uma média móvel com o comprimento, método e preço aplicado escolhidos.
2. Manter os valores mais recentes da média móvel em um buffer para que os valores nos índices de barra `BarA` e `BarB` possam ser acessados mesmo quando um deslocamento horizontal de MA é utilizado.
3. Quando uma vela finalizada chega dentro da janela `[StartHour, EndHour)`:
   - Recuperar o valor de MA em `BarA + MaShift` e `BarB + MaShift`.
   - Se o valor em `BarA` excede o valor em `BarB` em mais de `DifferenceThreshold`, abrir uma posição comprada a menos que `ReverseSignals` esteja habilitado.
   - Se o valor em `BarA` é menor que o valor em `BarB` em mais de `DifferenceThreshold`, abrir uma posição vendida (ou uma posição comprada quando `ReverseSignals` está habilitado).
   - Caso contrário, a estratégia mantém o comportamento original do EA e aciona uma entrada vendida.
4. As ordens são sempre enviadas a mercado usando o `Volume` da estratégia. Quando `CloseOppositePositions` está habilitado, o tamanho solicitado é aumentado para neutralizar qualquer exposição oposta antes de estabelecer a nova posição.
5. Proteções opcionais de stop-loss e take-profit são anexadas através de `StartProtection`. As distâncias são expressas em pips, onde um pip equivale a `PriceStep` multiplicado por 10 para instrumentos cotados com 3 ou 5 dígitos decimais.

## Entradas
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Período de 1 minuto | Série de velas utilizada para cálculos e sinais. |
| `StopLossPips` | `int` | 30 | Distância do stop-loss em pips. Definir como 0 para desabilitar. |
| `TakeProfitPips` | `int` | 50 | Distância do take-profit em pips. Definir como 0 para desabilitar. |
| `StartHour` | `int` | 8 | Hora inclusiva (0-23) que marca o início da sessão de negociação. |
| `EndHour` | `int` | 21 | Hora exclusiva (0-23) que marca o fim da sessão de negociação. Deve ser maior que `StartHour`. |
| `DifferenceThreshold` | `decimal` | 0.0001 | Diferença absoluta mínima entre os valores de MA comparados que aciona um sinal direcional. |
| `BarA` | `int` | 0 | Índice da primeira barra usada para a comparação de MA (0 = vela atual). |
| `BarB` | `int` | 1 | Índice da segunda barra usada para a comparação de MA. |
| `ReverseSignals` | `bool` | `false` | Inverte as condições de alta e baixa. |
| `CloseOppositePositions` | `bool` | `false` | Se habilitado, aumenta o tamanho da ordem para fechar qualquer posição na direção oposta antes de abrir o novo trade. |
| `MaShift` | `int` | 0 | Deslocamento horizontal aplicado à linha da média móvel. Valores positivos acessam pontos de MA mais antigos. |
| `MaLength` | `int` | 55 | Período da média móvel. |
| `MaMethod` | `MovingAverageMethods` | `Exponential` | Método de suavização (`Simple`, `Exponential`, `Smoothed`, `Weighted`). |
| `AppliedPrice` | `AppliedPriceTypes` | `Median` | Preço usado como entrada de MA (`Close`, `Open`, `High`, `Low`, `Median`, `Typical`, `Weighted`). |

## Gestão de posições
- Definir o `Volume` da estratégia para controlar o tamanho base da operação. É combinado com a posição atual quando `CloseOppositePositions` está ativo.
- As proteções de stop-loss e take-profit são opcionais. Elas são anexadas apenas quando a respectiva distância em pips é maior que zero.

## Notas
- A janela de negociação funciona no tempo do instrumento; sinais fora de `[StartHour, EndHour)` são ignorados.
- Quando `MaShift` produz índices negativos, a estratégia aguarda até que histórico suficiente seja acumulado, espelhando o comportamento original do EA onde buffers deslocados podem retornar `EMPTY_VALUE`.
- Como o especialista original sempre tem como padrão uma ordem de venda quando o limite de diferença não é atingido, a estratégia convertida mantém a mesma lógica para plena fidelidade. Ajustar `DifferenceThreshold` se esse comportamento for indesejável.
