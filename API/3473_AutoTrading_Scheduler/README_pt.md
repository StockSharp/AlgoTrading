# Estratégia do Agendador de AutoTrading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia do AutoTrading Scheduler replica o consultor especialista EarnForex MetaTrader que alterna a opção "AutoTrading" de MetaTrader. A porta StockSharp mantém a conta estável fora das janelas de tempo definidas pelo usuário e retoma a negociação quando o relógio retrocede dentro de um intervalo permitido. Toda configuração é realizada através de strings legíveis, uma para cada dia da semana.

O módulo é intencionalmente independente de sinal: ele não abre novas negociações por conta própria. Em vez disso, supervisiona o estado comercial da estratégia anfitriã. Quando o agendador desativa a negociação automática, ele cancela todas as ordens ativas, opcionalmente nivela a posição atual e registra o evento por meio de `AddInfoLog` para que o aplicativo host possa reagir.

## Lógica Original

* Carrega um horário persistente com vários intervalos de tempo por dia da semana.
* Suporta bases de tempo locais ou de corretor/servidor.
* Verifica a programação a cada segundo através de um temporizador interno.
* Quando o relógio está fora de cada intervalo do dia da semana atual, ele desativa a negociação automática e pode, opcionalmente, fechar todas as negociações abertas e ordens pendentes.
* Reativa a negociação automática quando o relógio entra novamente em qualquer intervalo permitido.

## Notas de implementação

* A versão StockSharp armazena o cronograma analisado na memória e o recalcula sempre que o usuário edita um dos parâmetros de texto.
* Os intervalos de tempo aceitam vários formatos: `9-12`, `09:30-16:00`, `21.15-23.45`. Os minutos são opcionais e o padrão é `00` quando omitidos. Separe vários períodos com vírgulas.
* Um intervalo cujo final é igual a `00:00` permanece ativo até meia-noite (por exemplo, `22-0` significa 22:00:00 até 23:59:59). Usar `0-0` mantém a negociação habilitada durante todo o dia.
* Os intervalos de tempo cujo final é menor que o início são automaticamente transferidos para o dia seguinte, espelhando a lógica auxiliar do consultor especialista original.
* O cronômetro é executado a cada cinco segundos para equilibrar a capacidade de resposta e o uso de recursos.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `SchedulerEnabled` | `bool` | `false` | Interruptor mestre que ativa o horário. Quando desativada, a estratégia nunca interfere na negociação. |
| `ReferenceClock` | `TimeReference` | `Local` | Escolhe entre o relógio da máquina local e o horário da exchange/servidor fornecido pelo conector. |
| `ClosePositionsBeforeDisable` | `bool` | `true` | Quando o agendador desativa a negociação automática, ele primeiro cancela todas as ordens ativas e nivela a posição atual. |
| `MondaySchedule` | `string` | `""` | Lista separada por vírgulas de intervalos de negociação para segunda-feira. |
| `TuesdaySchedule` | `string` | `""` | Lista separada por vírgulas de intervalos de negociação para terça-feira. |
| `WednesdaySchedule` | `string` | `""` | Lista separada por vírgulas de intervalos de negociação para quarta-feira. |
| `ThursdaySchedule` | `string` | `""` | Lista separada por vírgulas de intervalos de negociação para quinta-feira. |
| `FridaySchedule` | `string` | `""` | Lista separada por vírgulas de intervalos de negociação para sexta-feira. |
| `SaturdaySchedule` | `string` | `""` | Lista separada por vírgulas de intervalos de negociação para sábado. |
| `SundaySchedule` | `string` | `""` | Lista separada por vírgulas de intervalos de negociação para domingo. |

Todos os parâmetros de agendamento aceitam a mesma sintaxe. Exemplo: `"09-12, 13:30-17:45, 22-0"`.

## Uso

1. Anexe a estratégia ao título ou portfólio desejado.
2. Insira um ou mais intervalos de tempo para os dias em que deseja negociar. Deixe um dia vazio para proibir a negociação durante todo o dia.
3. Habilite o agendador configurando `SchedulerEnabled = true`.
4. Decida se as posições devem ser niveladas automaticamente usando `ClosePositionsBeforeDisable`.
5. Monitore a saída do log: cada alternância grava uma mensagem com o motivo (janela aberta ou fechada).

Quando o horário atual está dentro de um intervalo permitido a estratégia define `IsAutoTradingEnabled = true`. Fora de cada intervalo, a propriedade muda para `false`, o módulo cancela as ordens de serviço, nivela a posição se configurada e registra a ação.

## Limitações conhecidas

* A estratégia supervisiona apenas o título único que lhe está associado. Portfólios multisímbolos requerem múltiplas instâncias de agendador ou um coordenador personalizado.
* O intervalo do temporizador pode ser ajustado dentro do código-fonte (`TimeSpan.FromSeconds(5)`) se uma granularidade diferente for necessária.
* A estratégia não persiste o agendamento no disco. Use os mecanismos de armazenamento de parâmetros do aplicativo host se a persistência for necessária.
