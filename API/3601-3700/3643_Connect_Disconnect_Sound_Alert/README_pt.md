# Estratégia de alerta sonoro de conexão e desconexão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de alerta sonoro Connect Disconnect** monitora continuamente o status da conexão do conector estratégico e registra cada transição entre os estados online e offline. O especialista MQL5 original reproduziu arquivos de áudio quando o terminal MetaTrader foi conectado ou desconectado. Essa conversão C# mantém a lógica central – detectando alterações de conexão – e expõe ganchos que permitem que o tempo de execução do StockSharp registre eventos e durações. A estratégia pode ser usada como um cão de guarda leve que informa a operadora sobre problemas de conectividade sem fazer nenhum pedido.

## Principais recursos
- Pesquisa periodicamente o estado do conector usando um intervalo configurável.
- Detecta eventos de conexão e desconexão e grava entradas de log detalhadas.
- Registra quanto tempo o terminal permaneceu online ou offline (opcional).
- Ignora os sons de notificação na primeira verificação para espelhar o comportamento MQL.

## Parâmetros
| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `CheckIntervalSeconds` | `1` | Número de segundos entre verificações de status do conector. Deve ser maior que zero. |
| `LogDurations` | `true` | Quando habilitada, a estratégia registra o intervalo de tempo que a conexão permaneceu online ou offline após cada transição. |

Todos os parâmetros são expostos por meio de `StrategyParam<T>` para que possam ser modificados na IU ou durante a otimização.

## Como funciona
1. Quando a estratégia é iniciada, ela armazena o estado atual do conector e, opcionalmente, registra o status inicial.
2. Um `System.Threading.Timer` chama periodicamente um manipulador interno que compara o sinalizador de conexão atual com o valor anterior.
3. Se o estado mudou, a estratégia registra a transição. A primeira notificação é marcada como "inicial" e não representa um alerta sonoro real (correspondendo à lógica original do Expert Advisor).
4. Os registros de duração opcionais mostram quanto tempo durou o estado anterior, ajudando o operador a avaliar a estabilidade da conexão.
5. O cronômetro é descartado automaticamente quando a estratégia é interrompida ou reiniciada.

## Notas de uso
- Anexe a estratégia a qualquer terminal StockSharp habilitado para conector. Ele não interage com dados de mercado nem faz pedidos.
- Mantenha o intervalo de pesquisa padrão para monitoramento quase em tempo real. Aumente o valor se precisar apenas de atualizações grosseiras.
- A estratégia usa o subsistema de registro StockSharp (`LogInfo`). Configure ouvintes de log ou painéis para ver as notificações.
- Para adicionar alertas sonoros reais, conecte um serviço de notificação em seu aplicativo host e reproduza o áudio quando as mensagens de registro chegarem.

## Considerações de segurança
- A estratégia valida o intervalo de pesquisa e lança uma exceção se não for positivo.
- Os retornos de chamada do temporizador usam a estratégia `CurrentTime` para garantir carimbos de data/hora consistentes mesmo quando a reprodução de dados históricos é usada.
- Todos os recursos são liberados na parada/redefinição para evitar temporizadores de segundo plano após a estratégia ser desativada.
