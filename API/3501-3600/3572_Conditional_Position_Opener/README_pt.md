# Estratégia de abertura de posição condicional
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de abertura de posição condicional** reproduz o comportamento do script MetaTrader original *"Abrir uma posição de compra se não houver posição aberta"*. A lógica é intencionalmente simples: quando as mudanças manuais permitem o lado longo ou curto, a estratégia envia uma ordem de mercado apenas se não houver exposição aberta nessa direção. Isto evita entradas duplicadas e mantém a posição alinhada com o lado selecionado.

A porta StockSharp mantém a implementação neutra como corretor, contando com a assinatura de vela de alto nível da estrutura e o auxiliar de proteção integrado. As distâncias de stop-loss e take-profit são fornecidas em unidades pip (etapas de preço) para que possam ser adaptadas a qualquer instrumento.

## Lógica da estratégia
1. Assine a série de velas configurada para atuar como uma pulsação de tempo.
2. Em cada vela finalizada, verifique a posição líquida atual.
3. Se a opção longa estiver habilitada e a posição for plana ou curta, envie uma ordem de compra a mercado.
4. Se a chave curta estiver habilitada e a posição for plana ou longa, envie uma ordem de venda ao mercado.
5. As ordens de proteção são gerenciadas automaticamente por meio de `StartProtection`, que converte as distâncias do pip em compensações de preços reais.

Como StockSharp usa posições líquidas, habilitar ambos os lados ao mesmo tempo abrirá primeiro a negociação longa e, em seguida, se ainda estiver estável após o preenchimento, a negociação curta. Isso reflete a intenção do código MQL que evitou vários pedidos por direção.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Volume` | `1` | Tamanho do pedido para cada entrada no mercado. |
| `StopLossPips` | `100` | Distância de stop-loss expressa em etapas de preço. Defina como zero para desativar. |
| `TakeProfitPips` | `200` | Distância de lucro expressa em etapas de preço. Defina como zero para desativar. |
| `EnableBuy` | `false` | Quando `true` a estratégia pode abrir posições longas se não existir exposição longa. |
| `EnableSell` | `false` | Quando `true` a estratégia pode abrir posições curtas se não existir exposição curta. |
| `CandleType` | `1 minute timeframe` | Série de velas que impulsiona a avaliação periódica. |

## Notas
- As distâncias são convertidas em incrementos de preço reais usando o `PriceStep` do instrumento. Se a bolsa não reportar isso, o valor bruto do pip será usado como uma compensação absoluta.
- `StartProtection` anexa automaticamente ordens de stop-loss e take-profit após cada preenchimento, portanto, nenhum gerenciamento manual de pedidos é necessário.
- A estratégia concentra-se no acionamento de estilo manual e pretende ser um modelo para execução discricionária por meio de parâmetros.
