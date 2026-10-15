# Estratégia de calculadora de comissão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia da Calculadora de Comissão** é uma estratégia utilitária que reflete o script MetaTrader original. Ele envia uma única ordem discricionária usando o modo de execução selecionado (mercado, limite ou stop) e mede a comissão da corretora aplicada a cada preenchimento resultante. A estratégia armazena a comissão acumulada e imprime um relatório final com o saldo inicial, taxas totais e saldo ajustado pelas taxas quando termina.

Ao contrário das estratégias convencionais baseadas em sinais, não são necessários dados ou indicadores de mercado. A estratégia centra-se na contabilização automatizada de taxas para execuções manuais ou semimanuais.

## Lógica de negociação
1. Quando a estratégia é iniciada, ela captura o saldo inicial da carteira e configura o volume de negociação padrão.
2. Os níveis protetores opcionais de stop-loss e take-profit são ativados por meio de `StartProtection` quando o preço de entrada e os preços-alvo são válidos. As distâncias são calculadas em unidades de preço absoluto, imitando a implementação MQL.
3. O modo de ordem configurado é executado exatamente uma vez. Se os parâmetros forem inconsistentes (por exemplo, falta de preço de entrada para ordens limitadas), a estratégia registra o problema e ignora o envio da ordem.
4. Cada negociação própria recebida através de `OnNewMyTrade` é processada para calcular a taxa de comissão usando a taxa percentual configurada.
5. A estratégia agrega todas as comissões, lembra a taxa mais recente e registra um resumo detalhado no stop.

A implementação pressupõe que a taxa de corretagem é proporcional a `price × volume × commissionRate / 100`. Ajuste a taxa para corresponder ao local que está sendo modelado.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Quantity` | `0.001` | Volume de negociação enviado por métodos auxiliares (`BuyMarket`, `SellLimit`, etc.). |
| `EntryPrice` | `31365` | Preço utilizado para ordens limite ou stop e para cálculo de distâncias de proteção. |
| `StopLossPrice` | `31200` | Preço que define a distância do stop loss. Uma distância não positiva desativa a proteção stop-loss. |
| `TakeProfitPrice` | `32100` | Preço que define a distância do take-profit. Uma distância não positiva desativa a proteção do lucro. |
| `CommissionRate` | `0.04` | Taxa de comissão expressa em percentagem do nocional negociado. |
| `Mode` | `None` | Tipo de ordem a ser executada quando a estratégia for iniciada. Opções: `None`, `MarketBuy`, `MarketSell`, `BuyLimit`, `SellLimit`, `BuyStop`, `SellStop`. |

## Notas e melhores práticas
- Inicie a estratégia em um portfólio que suporte colocação manual de pedidos; nenhuma assinatura de dados é necessária.
- Certifique-se de que o modelo de comissão do corretor corresponda ao parâmetro `CommissionRate` para evitar subestimar ou superestimar as taxas.
- Para ordens pendentes, defina `EntryPrice` para um nível válido antes de lançar a estratégia; caso contrário, o pedido não será enviado.
- Quando os níveis de proteção são ativados, a estratégia instrui o conector a usar saídas de mercado no momento do acionamento para imitar de perto o comportamento original MQL.

## Relatório de resultados
Quando `OnStopped` é invocado, a estratégia registra:
- Instantâneo do saldo inicial (tirado quando a estratégia foi iniciada).
- Taxas de corretagem agregadas para todas as negociações processadas.
- Saldo final ajustado subtraindo-se as taxas acumuladas.

Isso torna a estratégia adequada para análises hipotéticas rápidas e para validar cronogramas de comissões de corretores durante backtests.
