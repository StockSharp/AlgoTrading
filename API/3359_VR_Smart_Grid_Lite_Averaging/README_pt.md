# Estratégia de média VR Smart Grid Lite
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia VR Smart Grid Lite Averaging é um sistema de média de grade que segue o consultor especialista MetaTrader 5 original. O algoritmo abre ordens de mercado na direção da vela de alta ou baixa mais recente e constrói uma escada estilo martingale sempre que o preço se move contra a posição. Distâncias, volumes e lógica de saída podem ser ajustados para corresponder à implementação original do MQL.

## Lógica de negociação
- A cada vela concluída, a estratégia verifica sua direção.
  - Uma vela de alta permite uma nova ordem de compra se o preço atual estiver pelo menos `Order Step (pips)` abaixo da entrada de compra mais baixa existente.
  - Uma vela de baixa permite uma nova ordem de venda se o preço atual estiver pelo menos `Order Step (pips)` acima da entrada de venda mais alta existente.
- A primeira ordem para cada lado usa `Start Volume`. Cada pedido adicional duplica o volume do pedido mais distante daquele lado, enquanto `Max Volume` limita o tamanho absoluto.
- Quando existe apenas uma posição em um lado, a negociação é fechada quando o preço atinge a distância `Take Profit (pips)`.
- Com duas ou mais posições a lógica de fechamento depende do `Close Mode` selecionado:
  - **Média** – fecha os pedidos mais altos e mais baixos quando o preço atinge a média ponderada mais `Minimal Profit (pips)`.
  - **PartialClose** – fecha totalmente o pedido mais baixo e reduz o pedido mais alto em `Start Volume` quando o preço atinge a meta combinada.

## Gestão de risco
- Os volumes são ajustados ao `MinVolume`, `MaxVolume` e `StepVolume` da corretora para evitar rejeição.
- A chamada `StartProtection()` integrada garante que a proteção da conta StockSharp seja ativada antes da negociação.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| `Take Profit (pips)` | Distância alvo para posições abertas únicas. |
| `Start Volume` | Volume para o pedido inicial em cada direção. |
| `Max Volume` | Volume máximo permitido por pedido (0 desativa o limite). |
| `Close Mode` | Escolha entre saídas médias ou fechamentos parciais. |
| `Order Step (pips)` | Movimento adverso mínimo antes de adicionar uma nova ordem. |
| `Minimal Profit (pips)` | Buffer de lucro extra adicionado à saída média. |
| `Candle Type` | Série de velas usada para geração de sinal. |

## Notas
- A estratégia utiliza apenas ordens de mercado; ordens pendentes do EA original são emuladas avaliando as condições de cada vela.
- A implementação mantém o estado por pedido para imitar o gerenciamento baseado em tickets do MetaTrader, incluindo fechamentos parciais e saídas seletivas.
- Configure o tipo de vela e o tamanho do pip do símbolo para corresponder ao período de tempo usado no script MQL para um comportamento consistente.
