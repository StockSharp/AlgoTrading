# Negociação de rede em mercado volátil
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o MetaTrader especialista "Gridtrading_at_volatile_market.mq4" usando o StockSharp API de alto nível. Ele negocia em torno de Donchian limites de canal detectados em um período de tempo mais alto, enquanto confirma entradas com padrões envolventes no período de negociação. Uma vez que uma grade está ativa, a estratégia adiciona ordens médias quando o preço se estende por múltiplos do período de tempo mais alto ATR e sai quando o lucro do portfólio ou as metas de redução são atingidas.

## Como funciona
1. Dois fluxos de velas são usados: o período de negociação selecionado pelo usuário e um período de tempo mais alto derivado automaticamente dele (M1→M5→M15→M30→H1→H4→D1).
2. No prazo superior a estratégia calcula:
   - `ATR(20)` para dimensionar o espaçamento da grade.
   - `SMA(SlowMaLength)` para filtrar a tendência junto com RSI.
   - `DonchianChannels(20)` para níveis de suporte e resistência.
3. No período de negociação, ele rastreia as duas últimas velas concluídas para detectar padrões envolventes de alta ou baixa.
4. Uma grade longa começa quando a vela anterior toca a banda inferior Donchian, forma um padrão envolvente de alta e RSI confirma condições de sobrevenda (`RSI < 35` enquanto o preço está acima do período de tempo superior SMA). Uma grade curta reflete essas regras na banda superior com `RSI > 65`.
5. Após a primeira ordem de mercado a estratégia mantém o preço inicial como âncora. Se o preço se mover contra a posição em `2 * ATR` para a etapa atual da grade, ele adiciona outra ordem com volume multiplicado por `GridMultiplier`.
6. A grade é fechada e todos os pedidos são cancelados quando:
   - O PnL combinado (realizado + não realizado) excede `TakeProfitFactor * total grid volume`.
   - O rebaixamento fica abaixo de `-MaxDrawdownFraction * initial portfolio value`.

## Parâmetros
- **TakeProfitFactor** – lucro múltiplo do volume total da grade necessário para fechar a grade (padrão `0.1`).
- **SlowMaLength** – período do intervalo de tempo maior SMA usado para filtragem (padrão `50`).
- **GridMultiplier** – fator geométrico aplicado a cada ordem de média adicional (padrão `1.5`).
- **BaseOrderVolume** – volume da primeira ordem na grade (padrão `0.1`).
- **MaxDrawdownFraction** – perda máxima relativa ao valor inicial do portfólio antes do fechamento forçado da grade (padrão `0.8`).
- **CandleType** – período de negociação. O prazo mais alto é inferido automaticamente.

## Notas
- Apenas velas fechadas são processadas para evitar repinturas.
- A estratégia depende de cotações de compra/venda disponíveis para avaliar o PnL aberto; se apenas forem fornecidos os últimos preços comerciais, a aproximação poderá ser menos precisa.
- Quando as informações da carteira não estão disponíveis, a proteção contra rebaixamento é ignorada, permitindo que a grade funcione até que a meta de lucro seja atingida ou a posição seja fechada manualmente.
