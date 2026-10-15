# Estratégia TugbaGold
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

TugbaGold é um consultor especialista em média baseado em grade que se origina de MetaTrader 5. A estratégia convertida recria seu dimensionamento de posição martingale e lógica de gerenciamento de cesta usando o StockSharp de alto nível de API. O sistema coloca novas ordens sempre que a vela anterior fecha com impulso direcional e constrói progressivamente uma grade de posições espaçadas por uma distância configurável. As saídas de média são executadas bloqueando os lucros nas posições extremas ou fechando parcialmente a cesta, dependendo do modo selecionado.

## Como funciona

1. A estratégia avalia velas concluídas a partir do parâmetro `CandleType`. Os sinais usam a vela *anterior*, correspondendo à lógica MT5 original.
2. Uma vela de alta permite a colocação de uma nova ordem de compra. Uma vela de baixa permite uma nova ordem de venda.
3. Os pedidos serão adicionados somente se a distância do melhor preço existente naquela direção exceder `PointOrderStepPips`.
4. O primeiro pedido usa `StartVolume`. As entradas subsequentes duplicam o volume da posição mais favorável, respeitando `MaxVolume` e os limites da corretora.
5. Uma vez que existam pelo menos duas posições, a estratégia calcula preços-alvo que incluem o buffer `MinimalProfitPips`. O cálculo difere de acordo com o modo de saída:
   - **Média** – média ponderada das posições extremas mais o buffer de lucro.
   - **Parcial** – combinação dos piores e melhores tickets onde o pior ticket usa `StartVolume` e o melhor usa seu tamanho real.
6. Quando as metas são atingidas, a estratégia fecha as ordens correspondentes:
   - **Modo médio** – fecha totalmente ambas as entradas extremas.
   - **Modo parcial** – fecha completamente a pior entrada e reduz a melhor entrada em `StartVolume`.
7. Posições autônomas únicas usam `TakeProfitPips` para sair quando o preço atinge a distância configurada.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `TakeProfitPips` | Distância de take-profit aplicada quando apenas uma posição está aberta. Defina como `0` para desativar. |
| `StartVolume` | Volume inicial para a primeira ordem em uma sequência de grade. |
| `MaxVolume` | Volume máximo de pedidos. `0` mantém a sequência de duplicação ilimitada. |
| `CloseMode` | Modo de saída: `Average` (fechar ambos os extremos) ou `Partial` (fechar parcial + total). |
| `PointOrderStepPips` | Distância mínima em pips antes que uma nova ordem média possa ser adicionada. |
| `MinimalProfitPips` | Buffer de lucro adicional adicionado às metas médias. |
| `CandleType` | Série de velas usada para avaliação de sinal. |

## Gestão de posição

- As etapas de preço são derivadas de `Security.PriceStep`. Se não estiver disponível, um padrão de `0.0001` será usado.
- Os volumes são automaticamente normalizados para as restrições mínimas, máximas e de etapas do corretor.
- A estratégia rastreia as posições preenchidas internamente e emite ordens de mercado (`BuyMarket` / `SellMarket`) ao fechar partes da cesta.
- A proteção é ativada automaticamente por meio de `StartProtection()` assim que a estratégia é iniciada.

## Notas e limitações

- A implementação pressupõe atendimento imediato para ordens de mercado, semelhante ao ambiente MT5.
- Os sinais de média dependem das melhores cotações de compra/venda atuais; garantir que os dados do Nível 1 estejam disponíveis para uma execução precisa.
- Como as saídas são impulsionadas pela lógica estratégica, os níveis de stop loss do especialista original não são recriados.
- Utilize uma gestão de risco cautelosa: o dimensionamento do martingale pode levar a uma grande exposição se as tendências persistirem.

## Detalhes da conversão

- As fórmulas de média e os ajustes da cesta refletem o código-fonte original.
- A seleção de posição (melhores/piores tickets) é reproduzida rastreando os preços de abertura mais altos e mais baixos em cada direção.
- Toda a lógica é executada dentro da assinatura da vela usando o API de alto nível de StockSharp sem recorrer ao acesso a dados de baixo nível.
