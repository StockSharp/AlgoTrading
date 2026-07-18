# Estratégia de Lista de Posições
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Lista de Posições** reproduz o comportamento do script original do MetaTrader ao imprimir periodicamente as posições atuais do portfólio no log da estratégia. É um helper de monitoramento exclusivo que nunca coloca ordens. Em vez disso, constrói um snapshot das posições abertas para que o operador possa inspecionar símbolo, direção, tamanho, preço de entrada e lucro atual diretamente do Designer ou dos logs do StockSharp.

## Recursos principais
- Relatório de posições orientado por temporizador com o primeiro snapshot entregue imediatamente após o início da estratégia.
- Filtragem opcional pelo ativo da estratégia ou por identificador de estratégia (o análogo do número mágico do MetaTrader).
- Saída de log detalhada incluindo o identificador de posição, último tempo de mudança, lado, quantidade, preço médio e lucro.
- Processamento thread-safe que evita sobreposições de callbacks do temporizador quando o ambiente está ocupado.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | --------- | ------ |
| `StrategyIdFilter` | Identificador de estratégia a pular. Quando deixado vazio, todas as posições são reportadas. | String vazia |
| `SelectionMode` | Controla se posições de cada símbolo ou apenas de `Strategy.Security` são reportadas. | `AllSymbols` |
| `TimerInterval` | Intervalo entre snapshots consecutivos de posições. | 6 segundos |

## Como funciona
1. Durante `OnStarted`, a estratégia verifica que um portfólio está anexado e que o intervalo do temporizador é positivo.
2. Um `System.Threading.Timer` é criado com zero de atraso para que o primeiro relatório seja produzido imediatamente e depois repetido no intervalo configurado.
3. Cada tick do temporizador chama `ProcessPositions`, que itera sobre `Portfolio.Positions`, aplica os filtros opcionais de símbolo e identificador de estratégia, e acrescenta linhas formatadas a um `StringBuilder`.
4. Quando pelo menos uma posição passa os filtros, a tabela montada é escrita no log com `LogInfo`. Se nada corresponder, uma notificação concisa é registrada em vez disso.
5. As sobreposições do temporizador são evitadas com um guarda interlocked para que a I/O lenta não possa acionar execuções concorrentes.

## Notas de uso
- Atribua tanto `Portfolio` quanto `Connector` antes de iniciar a estratégia. Se `SelectionMode` estiver configurado como `CurrentSymbol`, também configure `Strategy.Security` para o instrumento que deseja monitorar.
- Para emular o filtro `magic` do MetaTrader, preencha `StrategyIdFilter` com o valor de string usado como `StrategyId` quando outras estratégias enviam ordens. Essas posições serão excluídas do relatório.
- A estratégia nunca modifica posições ou registra ordens, tornando-a segura para executar ao lado de lógica de trading ao vivo como um widget informacional.
- A saída do log é agrupada sob o cabeçalho de coluna `Idx | Symbol | PositionId | LastChange | Side | Quantity | AvgPrice | PnL` para que possa ser facilmente analisado por ferramentas externas, se necessário.

## Diferenças em relação à versão MQL
- O MetaTrader usa um número `magic` de 64 bits sem sinal. As posições do StockSharp expõem o identificador de estratégia como uma string, portanto o filtro aceita valores textuais.
- Em vez de escrever no comentário do gráfico, esta portagem registra o snapshot via `LogInfo`, que é visível no Designer, Runner ou qualquer listener de log.
- A versão do StockSharp protege contra invocações sobrepostas do temporizador para permanecer responsiva sob carga pesada.
- Os timestamps dependem de `Position.LastChangeTime`, que reflete as atualizações de posição do StockSharp, enquanto o script MQL exibia o tempo de criação do ticket.
