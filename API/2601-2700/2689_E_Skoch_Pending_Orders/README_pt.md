# Estratégia de Ordens Pendentes E Skoch
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral

A **Estratégia de Ordens Pendentes E Skoch** recria o assessor especialista original do MetaTrader que aguarda uma nova barra, analisa os dois máximos e mínimos mais recentes no período de trading e no diário, e coloca ordens de rompimento pendentes. O objetivo é capturar momentum quando o mercado rompe através da barra anterior após um pullback de curto prazo confirmado pela tendência diária.

A implementação do StockSharp mantém as ideias originais, mas usa recursos de API de alto nível como assinaturas de candles, ordens de proteção automáticas e parâmetros de estratégia. A versão em C# está armazenada na pasta `CS/` e ainda não há porta Python disponível.

## Lógica de Trading

1. Em cada candle concluído, a estratégia recupera os máximos e mínimos dos dois candles anteriores no período de trabalho e dos dois candles diários anteriores.
2. Se o último máximo diário for menor que o de dois dias atrás **e** o máximo intradiário anterior for menor que o anterior, a estratégia coloca um **buy stop** acima do último máximo intradiário mais um buffer configurável.
3. Se o último mínimo diário for maior que o de dois dias atrás **e** o mínimo intradiário anterior for maior que o anterior, a estratégia coloca um **sell stop** abaixo do último mínimo intradiário menos um buffer configurável.
4. Cada ordem pendente define níveis individuais de stop-loss e take-profit. Quando uma entrada é acionada, a estratégia envia imediatamente ordens de stop e limite de proteção para a posição aberta.
5. Quando não há posições ou ordens ativas, a estratégia registra o capital atual como linha de base. Se o capital da conta crescer a porcentagem configurada relativa a essa linha de base, todas as posições são fechadas e as ordens de proteção são canceladas.
6. O bloqueio opcional (`CheckExistingTrade`) impede novas entradas enquanto qualquer posição estiver aberta, espelhando o parâmetro de entrada original "CheckTrade".

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `CandleType` | Período principal usado para sinais. Padrão: candles de 1 hora. |
| `TakeProfitBuyPips` / `StopLossBuyPips` | Compensações de lucro e perda do lado comprado medidas em pips. |
| `TakeProfitSellPips` / `StopLossSellPips` | Compensações de lucro e perda do lado vendido medidas em pips. |
| `IndentHighPips` / `IndentLowPips` | Distância em pips do último máximo ou mínimo usada para colocar ordens de stop. |
| `CheckExistingTrade` | Quando verdadeiro, novas ordens são ignoradas enquanto qualquer posição estiver aberta. |
| `PercentEquity` | Percentual de ganho no capital necessário para sair de todas as posições. |
| `Volume` | Tamanho da ordem (padrão 0,01 lote para corresponder ao assessor especialista original). |

## Gestão de Risco

- Ordens buy stop colocam um stop-loss abaixo do preço de entrada e um take-profit acima.
- Ordens sell stop colocam um stop-loss acima do preço de entrada e um take-profit abaixo.
- Ordens de proteção são automaticamente canceladas quando a posição fecha ou quando um novo conjunto de proteção é criado.
- A verificação de crescimento do capital age como um "disjuntor" global para garantir lucros antes que o trading seja retomado.

## Notas

- A estratégia requer tanto o período de trading quanto candles diários, então certifique-se de que os dados para ambas as assinaturas estejam disponíveis no Designer ou durante backtests.
- A conversão de pips ajusta automaticamente os símbolos que usam preços de pip fracionários (3 ou 5 dígitos decimais) multiplicando o passo de preço por 10.
- A lógica assume uma única posição agregada; a exposição simultânea comprada e vendida é intencionalmente evitada quando `CheckExistingTrade` está habilitado.
