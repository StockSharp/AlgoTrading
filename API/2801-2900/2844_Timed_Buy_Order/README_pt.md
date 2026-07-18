# Estratégia de Ordem de Compra Temporizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Ordem de Compra Temporizada** replica o consultor especialista MetaTrader `buy_order.mq4`, que envia um fluxo de ordens de compra a mercado impulsionadas por um temporizador de um segundo. O port do StockSharp mantém o mesmo ritmo de negociação: aguarda até que o temporizador se alinhe com o segundo esperado dentro do minuto atual e então envia a próxima ordem. Após um número predefinido de execuções, a estratégia para automaticamente.

Esta implementação depende do serviço de `Timer` de alto nível do StockSharp em vez de loops manuais. Nenhum indicador de mercado ou assinatura de candles é necessário, tornando a lógica determinista e orientada ao tempo.

## Lógica central
1. Quando a estratégia inicia, ela ativa a proteção de risco via `StartProtection()` e inicia um temporizador com o intervalo configurado (padrão: um segundo).
2. Cada callback do temporizador verifica se a estratégia está online e autorizada a negociar, e se o segundo atual da bolsa corresponde ao valor esperado na sequência.
3. Se todas as verificações forem bem-sucedidas, a estratégia envia uma ordem de compra a mercado com o volume configurado.
4. O processo se repete até que o número alvo de ordens tenha sido enviado, após o que a estratégia para.

O comportamento de sincronização de segundos espelha o especialista MQL original: a primeira ordem só é despachada quando o componente de segundos atinge zero, e cada ordem subsequente é vinculada ao próximo valor de segundo.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| ---- | ---- | ------- | ----------- |
| `OrderVolume` | `decimal` | `0.01` | Quantidade para cada ordem de compra a mercado. Uma guarda de validação para a estratégia se o valor não for positivo. |
| `OrdersToPlace` | `int` | `60` | Número total de ordens de compra sequenciais a enviar antes de parar. |
| `Interval` | `TimeSpan` | `1s` | Atraso entre callbacks do temporizador. Mantê-lo em um segundo reproduz melhor o tempo MQL, mas outros valores são possíveis para experimentação. |

Todos os parâmetros são expostos através de objetos `StrategyParam<T>` do StockSharp, para que possam ser otimizados ou configurados a partir de ferramentas de UI.

## Fluxo de execução
- **Inicialização** – resetar os contadores em `OnReseted()` garante um estado limpo ao reiniciar ou re-otimizar.
- **Início** – em `OnStarted()` o temporizador começa e os contadores são resetados; a proteção é habilitada uma vez por ciclo de vida.
- **Tick do temporizador** – o método `OnTimer()` realiza as verificações de sequenciamento, registra a ordem de saída e para a estratégia quando a ordem final é enviada.
- **Conclusão** – o auxiliar `CompleteStrategy()` previne tentativas de encerramento duplicadas e chama `Stop()` exatamente uma vez.

## Notas de conversão
- A função MQL `EventSetTimer(1)` é mapeada para `Timer.Start(TimeSpan.FromSeconds(1), OnTimer)`.
- Comentários de ordens e números mágicos usados no MetaTrader não têm equivalentes diretos no StockSharp, então o logging é usado para rastrear o progresso.
- A estratégia mantém o conceito de "60 ordens por minuto" fazendo coincidir o componente de segundos em vez de contar disparos do temporizador.

## Dicas de uso
1. Atribua o ativo e portfólio desejados antes de iniciar a estratégia.
2. Ajuste `OrderVolume` para corresponder ao tamanho de lote do instrumento e às regras do broker.
3. Se precisar de menos ordens, reduza `OrdersToPlace`; para desabilitar completamente o ritmo baseado em segundos, defina `Interval` para qualquer valor e remova a correspondência de segundos no código (modificação avançada).
4. Monitore a saída de log para rastrear envios de ordens e garantir que o alinhamento do temporizador se comporte como esperado.

## Limitações
- A estratégia apenas compra; não há lógica de saída além de intervenção manual ou stops de proteção gerenciados pelo broker.
- A colocação de ordens é limitada pela precisão do serviço de temporizador fornecido pela conexão e sistema operacional; grandes atrasos podem dessincronizar a sequência.

## Arquivos
- `CS/TimedBuyOrderStrategy.cs` – implementação principal em C#.
- `README_zh.md` – documentação em chinês.
- `README_ru.md` – documentação em russo.

Um port em Python é intencionalmente omitido de acordo com as instruções do projeto; crie-o mais tarde se necessário.
