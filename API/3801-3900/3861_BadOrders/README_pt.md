# Estratégia de BadOrders
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia BadOrders** é uma versão direta do MetaTrader 4 consultor especialista `BadOrders.mq4`. O script original foi escrito intencionalmente para demonstrar como o gerenciamento incorreto de pedidos leva a negociações rejeitadas. Em cada entrada, marque:

1. Fecha à força a posição aberta mais recentemente ao preço de oferta atual.
2. Coloca um novo stop de compra 100 pontos acima do lance.
3. Modifica imediatamente a ordem pendente para ficar 100 pontos *abaixo* do lance, violando as regras de distância do corretor e provocando um erro.

A versão StockSharp reproduz esse comportamento com o API de alto nível. Ele assina cotações de Nível 1 para monitorar o melhor lance e reproduz o mesmo ciclo de fechamento-posição-invalidação sempre que uma cotação chega.

## Detalhes de implementação
- **Fluxo de dados**: `SubscribeLevel1()` é usado porque o script MT4 reage a cada tick em vez de conclusões de velas.
- **Gerenciamento de pedidos**: as posições abertas são fechadas com o auxiliar `ClosePosition()`. As paradas pendentes são gerenciadas por meio de `BuyStop()` e `ReRegisterOrder()` para que possamos mover imediatamente a ordem de parada para um preço ilegal, imitando o fluxo de trabalho interrompido do código-fonte.
- **Normalização de preços**: Todos os preços são normalizados via `Security.ShrinkPrice()` e o conceito MetaTrader de `Point` é emulado através do instrumento `PriceStep`. Quando nenhum tamanho de tick está disponível, a estratégia volta para `0.0001`.
- **Lógica de proteção**: antes de chamar `ClosePosition()` o código verifica as ordens de liquidação existentes para evitar o empilhamento de solicitações de saída duplicadas.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `DistancePoints` | Distância em MetaTrader “pontos” adicionados acima e abaixo do lance atual ao colocar ou registrar novamente a ordem stop. | `100` |

## Resumo do comportamento
- Sempre que a oferta muda, a estratégia tenta nivelar qualquer posição aberta.
- Um stop de compra é enviado em `bid + DistancePoints * PointValue` após o fechamento da posição.
- O mesmo pedido é imediatamente registrado novamente em `bid - DistancePoints * PointValue`, o que viola as regras da exchange e deverá falhar — refletindo precisamente os erros intencionais em `BadOrders.mq4`.

> **Nota**: Este projeto existe apenas para paridade com a amostra MT4 e não se destina à negociação ao vivo.
