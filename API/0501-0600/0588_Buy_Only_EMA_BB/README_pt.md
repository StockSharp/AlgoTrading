# Estratégia de Somente Compra com EMA e BB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre uma posição comprada quando o preço fecha acima da EMA.
O stop loss inicial é colocado na banda inferior de Bollinger e se move para a EMA se o preço fechar acima da banda superior.
O take profit é definido usando uma relação recompensa/risco baseada na distância até a banda.
Após o take profit ser atingido, a estratégia aguarda o preço cruzar abaixo da EMA antes de permitir uma nova entrada.

## Detalhes
- **Critérios de entrada:** Fechamento acima da EMA sem bloqueio ativo e sem posição aberta.
- **Comprado/Vendido:** Somente comprado.
- **Critérios de saída:** O preço cruza abaixo do nível de stop ou atinge o take profit.
- **Stops:** Stop inicial na banda inferior, deslocando-se para a EMA após um movimento forte.
- **Valores padrão:** Comprimento EMA = 40, desvio de banda = 0.7, relação recompensa/risco = 3.
