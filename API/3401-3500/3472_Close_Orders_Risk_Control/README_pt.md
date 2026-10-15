# Estratégia de controle de risco de pedidos fechados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Fechamento de Pedidos** é um utilitário de gerenciamento de risco que reflete o comportamento do consultor especialista MQL original *CloseOrders.mq4*. Ele monitora continuamente os lucros e perdas flutuantes das posições abertas e liquida automaticamente as ordens correspondentes assim que a meta de lucro ou o limite de perda for atingido. Isto o torna adequado para proteger um portfólio ou sincronizar saídas em múltiplas estratégias.

## Como funciona
1. A estratégia assina uma série de velas configuráveis (1 minuto por padrão) e avalia o PnL flutuante atual sempre que uma vela fecha.
2. O PnL flutuante é calculado para as posições ativas da carteira. Quando um número mágico é fornecido, apenas as posições cujo `StrategyId` interno corresponda ao valor configurado são incluídas.
3. Se o lucro flutuante for igual ou superior ao valor alvo, todas as ordens e posições correspondentes serão fechadas.
4. Se o lucro flutuante cair abaixo da perda de corte configurada (um número negativo), a mesma rotina de liquidação é acionada para minimizar perdas adicionais.
5. As ordens ativas que satisfazem o filtro de número mágico são canceladas antes de nivelar as posições para garantir que nenhuma nova exposição seja aberta durante a liquidação.

A rotina de liquidação continua em execução até que todas as posições correspondentes estejam estáveis, garantindo que os preenchimentos parciais sejam tratados normalmente.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| **Dinheiro de lucro alvo** | Lucro flutuante (na moeda da conta) que desencadeia a liquidação de ordens correspondentes. Deve ser maior que zero. |
| **Cortar dinheiro com perdas** | PnL flutuante negativo (na moeda da conta) que força a liquidação. Um valor de `0` desativa a saída baseada em perdas. |
| **Número Mágico** | Identificador de estratégia opcional. Deixe em branco para gerenciar todas as posições abertas; caso contrário, apenas as posições cujo `StrategyId` seja igual ao valor fornecido serão afetadas. |
| **Tipo de vela** | Série de velas usada para acionar verificações periódicas de lucros. Ajuste o prazo quando o monitoramento de frequência mais alta for necessário. |

## Notas de implementação
- O conceito de número mágico MQL é mapeado para os campos `UserOrderId`/`StrategyId` em StockSharp. Certifique-se de que as estratégias que devem ser gerenciadas utilizem o mesmo identificador.
- Tabulações são usadas para recuo e o arquivo segue a estrutura comum solicitada para estratégias convertidas.
- A estratégia cancela as ordens pendentes antes de enviar ordens de mercado para nivelar a exposição, evitando a reentrada imediata.
- A proteção inicial pode ser adicionada se a estratégia for combinada com componentes de negociação em tempo real que necessitam de tratamento de emergência.

## Dicas de uso
- Implemente a estratégia junto com estratégias de negociação que definem um `StrategyId` personalizado para centralizar a lógica de saída.
- Ajuste o parâmetro `Candle Type` para equilibrar a capacidade de resposta e o uso de recursos; prazos mais curtos proporcionam uma reação mais rápida às mudanças no PnL.
- Combine o utilitário com alertas para receber notificações sempre que a liquidação automatizada for executada.
