# Estratégia HistoryInfoEaStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **HistoryInfoEaStrategy** replica o utilitário MT4 "HistoryInfo" sobre o StockSharp. Em vez de desenhar texto no gráfico do MetaTrader, a estratégia escuta o fluxo `OnNewMyTrade` e agrega estatísticas de operações que correspondem a um filtro escolhido. Os valores agregados são expostos pela propriedade `LastSnapshot` e espelhados no log da estratégia para que uma GUI ou script de automação possa exibir o resumo no formato preferido.

A estratégia nunca registra suas próprias ordens. Ela foi projetada para rodar ao lado de outras estratégias automáticas ou manuais enquanto elas enviam ordens à corretora. Cada operação executada que satisfaz o filtro contribui para os totais.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `FilterType` | Modo de seleção que determina como operações são correspondidas. Valores suportados: `CountByUserOrderId`, `CountByComment`, `CountBySecurity`. |
| `MagicNumber` | `Order.UserOrderId` esperado. Usado apenas quando `FilterType` é igual a `CountByUserOrderId`. Deixe vazio para desabilitar este filtro. |
| `OrderComment` | Prefixo que deve corresponder a `Order.Comment`. Relevante apenas para o modo `CountByComment`. O valor padrão (`\"OrdersComment\"`) imita o placeholder do script MT4 e normalmente não corresponde a nenhuma ordem até ser substituído. |
| `SecurityId` | Identificador do instrumento (`Security.Id`) que deve corresponder quando `FilterType` é igual a `CountBySecurity`. O padrão (`\"OrdersSymbol\"`) é um placeholder. |

## Métricas agregadas
`LastSnapshot` é atualizado após cada operação correspondente. Ele contém:

- `FirstTrade` / `LastTrade` - timestamps da operação mais antiga e mais recente processadas.
- `TotalVolume` - volume preenchido acumulado expresso nas unidades de volume da operação (lotes, contratos etc.).
- `TotalProfit` - soma de `MyTrade.PnL` menos comissão reportada, fornecendo o lucro realizado na moeda da conta.
- `TotalPips` - lucro convertido para pips usando `Security.PriceStep`, `Security.StepPrice` e tratamento de dígitos semelhante ao MT4 (5/3 dígitos multiplicam o ponto por 10).
- `TradeCount` - número de operações que passaram pelo filtro.

A mesma informação é impressa no log da estratégia em uma única linha, emulando a saída `Comment()` do MT4 para inspeção rápida.

## Uso
1. Anexe a estratégia ao mesmo portfólio e ativo que outras estratégias usam para envio de ordens.
2. Escolha o `FilterType` desejado e preencha o parâmetro associado (magic number, prefixo de comentário ou identificador de ativo).
3. Inicie a estratégia. Assim que a primeira operação que corresponde aos critérios for preenchida, os totais ficam disponíveis por `LastSnapshot` e pelo log.
4. Os contadores são redefinidos automaticamente em cada reinício da estratégia ou reset manual.

> **Observação:** Para calcular totais em pips, a estratégia depende de metadados corretos do instrumento. Garanta que `Security.PriceStep` e `Security.StepPrice` estejam configurados na definição do board. Se qualquer valor estiver ausente, o contador de pips permanece em zero enquanto o valor de lucro continua acumulando.

## Notas de conversão
- O código MT4 iterava sobre `OrdersHistoryTotal()` em cada tick. No StockSharp, a estratégia reage a notificações `MyTrade` em tempo real, portanto não há polling e os cálculos atualizam imediatamente quando um fill chega.
- O MT4 armazenava lucro como `OrderProfit + OrderCommission + OrderSwap`. O StockSharp entrega lucro realizado via `MyTrade.PnL` e comissão separadamente; swap geralmente já está incluído no PnL. A versão subtrai comissão de `PnL` para manter consistência com o relatório original.
- Os placeholders de string (`\"OrdersComment\"`, `\"OrdersSymbol\"`) são preservados para lembrar os padrões originais. Substitua-os por valores reais antes de iniciar a estratégia se espera correspondências.
- A saída visual de gráfico do MT4 é substituída por dados estruturados (`LastSnapshot`) e linhas de log, para que integradores decidam como renderizar a informação.
- A estratégia evita propositalmente criar novas ordens, podendo ser iniciada em modo somente leitura para analisar fluxos de operações de terceiros sem interferir neles.

## Ideias de extensibilidade
- Assine as atualizações de `LastSnapshot` e encaminhe a informação para um dashboard ou coletor de telemetria.
- Estenda a classe com filtros adicionais (por exemplo, por portfólio ou tags de estratégia customizadas) se o conector fornecer os metadados relevantes.
- Combine a estratégia com um temporizador periódico para exportar resumos históricos para um relatório CSV/JSON.
