# Estratégia do Trailing Stop Trigger Manager
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Trailing Stop Trigger Manager** é uma versão StockSharp do MetaTrader consultor especialista `Trailing Sl.mq5`. O original EA
não abriu negociações por conta própria. Em vez disso, monitorou as posições já abertas com um *número mágico* correspondente e reforçou seus
níveis de stop-loss quando o mercado se moveu na direção desejada. Esta implementação C# reproduz esse comportamento usando
A estratégia de alto nível de StockSharp API, oferecendo gerenciamento transparente de trailing-stop que funciona com qualquer instrumento suportado por
StockSharp.

## Lógica final
1. Assina a carteira de pedidos para ler as últimas melhores ofertas e melhores cotações de venda.
2. Detecta se a estratégia mantém atualmente uma posição líquida longa ou curta.
3. Calcula o lucro flutuante usando o lado apropriado do mercado (melhor oferta para posições compradas, melhor oferta para posições vendidas).
4. Ativa o modo de rastreamento quando o lucro excede `TriggerPoints` (convertido em unidades de preço por meio de `PriceStep`).
5. Define o trailing stop na distância configurada `TrailingPoints` da cotação de mercado atual.
6. Move o trailing stop apenas em direção ao mercado para continuar obtendo lucro adicional.
7. Envia uma ordem de mercado para nivelar a posição assim que a melhor cotação atinge o nível de trailing stop calculado.

## Gerenciamento de pedidos e riscos
- A estratégia **não** envia pedidos de entrada iniciais. Gerencia apenas uma posição existente que pode ter sido aberta manualmente
ou por outra estratégia.
- As saídas de mercado são colocadas com `BuyMarket`/`SellMarket`, espelhando as chamadas `PositionModify` do código MetaTrader original.
- A distância de parada é automaticamente dimensionada com o `PriceStep` do instrumento, que preserva a configuração baseada em pontos de
o EA.
- Depois que a posição é fechada, o estado final é redefinido para que novas posições comecem do zero.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `TrailingPoints` | `int` | `1000` | Distância entre o preço atual e o trailing stop, medida em etapas de preço. |
| `TriggerPoints` | `int` | `1500` | Lucro mínimo nas etapas de preço necessário para começar a rastrear a posição. |

## Notas de uso
- Anexe a estratégia ao título cuja posição você deseja supervisionar. Ele começará imediatamente a rastrear o existente
exposição.
- Configure o `Volume` inicial da estratégia para corresponder ao tamanho da sua posição aberta. StockSharp usa posições líquidas, então o
estratégia sairá do lote inteiro quando o trailing stop for acionado.
- Se o corretor fornecer etapas de preços grosseiras, ajuste `TrailingPoints` e `TriggerPoints` de acordo para evitar saídas prematuras.
- A estratégia mantém seu estado inteiramente dentro de StockSharp, podendo ser combinada com qualquer sistema discricionário ou automatizado que
deixa a execução real do pedido para StockSharp.

## Diferenças do especialista MetaTrader original
- MetaTrader gerenciava posições separadas por ticket e as filtrava por *número mágico*. StockSharp trabalha com uma posição líquida por
segurança, eliminando a necessidade de filtragem de tickets.
- As entradas `Setloss`, `TakeProfit` e `Lots` não foram utilizadas no EA original. Eles são, portanto, omitidos no StockSharp
versão para manter a configuração focada no comportamento final.
- As modificações de ordem são substituídas por saídas diretas do mercado, que é a abordagem idiomática para compensação de contas em StockSharp.
