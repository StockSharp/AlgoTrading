# Estratégia Malr de Rompimento de Canal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia negocia rompimentos de um canal MALR (Moving Average Linear Regression) personalizado. O indicador MALR combina uma média móvel simples e uma média móvel linear ponderada para formar uma linha central. O desvio padrão do preço em relação a essa linha cria duas bandas externas.

Uma posição comprada é aberta quando a banda superior de rompimento cruza abaixo do preço de fechamento, indicando um rompimento para cima. Uma posição vendida é aberta quando a banda inferior de rompimento cruza acima do preço de fechamento, sinalizando um rompimento para baixo.

## Parâmetros

- `MaPeriod` – período para as médias móveis e o desvio padrão.
- `ChannelReversal` – largura do canal MALR interno medida em desvios padrão.
- `ChannelBreakout` – largura adicional para o canal externo de rompimento.
- `CandleType` – tipo de velas utilizado para os cálculos.

## Como funciona

1. Calcular SMA e LWMA do preço de fechamento.
2. Calcular a linha MALR `FF = 3 * LWMA - 2 * SMA`.
3. Medir o desvio padrão de `close - FF` sobre o mesmo período.
4. Derivar bandas de rompimento: `FF ± StdDev * (ChannelReversal + ChannelBreakout)`.
5. Entrar comprado quando a banda superior cruza de cima para baixo do fechamento.
6. Entrar vendido quando a banda inferior cruza de baixo para cima do fechamento.

A estratégia sempre fecha a posição oposta antes de abrir uma nova.
