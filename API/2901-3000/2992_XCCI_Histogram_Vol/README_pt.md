# Estratégia de Histograma Vol XCCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
Esta estratégia é um port StockSharp do consultor especialista MetaTrader `Exp_XCCI_Histogram_Vol`. Reproduz a lógica codificada por cores do indicador personalizado "XCCI Histogram Vol": um Commodity Channel Index (CCI) multiplicado pelo volume, suavizado por uma média móvel selecionável e comparado com limites dinâmicos. A implementação segue as diretrizes da API de alto nível, processa apenas velas fechadas e mantém a estrutura de posição dual original expondo volumes separados para as entradas primária e secundária.

## Fluxo de trabalho do indicador
1. Calcular o valor CCI com o período configurável.
2. Multiplicar o valor CCI pelo volume da vela.
3. Suavizar tanto a série CCI×Volume quanto o volume bruto com a média móvel escolhida (`Simple`, `Exponential`, `Smoothed`, `Weighted`, `Hull` ou `VolumeWeighted`).
4. Escalar quatro multiplicadores de limite definidos pelo usuário (HighLevel2/1 e LowLevel1/2) pelo volume suavizado.
5. Classificar o valor suavizado de CCI×Volume em uma das cinco zonas: `0` extremamente otimista, `1` otimista, `2` neutro, `3` pessimista, `4` extremamente pessimista.

A estratégia armazena a zona para cada vela terminada. O parâmetro `SignalBarOffset` controla quantas velas completamente fechadas aguardar antes de usar a zona nas decisões de negociação (espelhando o input original `SignalBar`).

## Regras de negociação
- **Saídas compradas**: se a zona avaliada for `3` ou `4`, cada posição comprada aberta é fechada.
- **Saídas vendidas**: se a zona avaliada for `1` ou `0`, cada posição vendida aberta é fechada.
- **Entrada comprada primária**: acionada quando a zona atual se torna `1` e a zona anterior (vela mais antiga) estava acima de `1`. Isso reflete a transição de território neutro/pessimista para a faixa otimista. O volume da ordem é `PrimaryEntryVolume` e fecha qualquer exposição vendida existente antes de virar.
- **Entrada comprada secundária**: acionada quando a zona atual se torna `0` e a zona anterior estava acima de `0`. Isso representa um surge para a região extremamente otimista e usa `SecondaryEntryVolume`.
- **Entrada vendida primária**: acionada quando a zona atual se torna `3` e a zona anterior estava abaixo de `3`, indicando um novo movimento para território pessimista. Usa `PrimaryEntryVolume` e fecha posições compradas primeiro se necessário.
- **Entrada vendida secundária**: acionada quando a zona atual se torna `4` e a zona anterior estava abaixo de `4`, sinalizando uma aceleração pessimista extrema. Usa `SecondaryEntryVolume`.

Os flags de entrada são redefinidos sempre que a posição líquida cruza zero para que o comportamento corresponda ao design de "dois números mágicos" do MetaTrader — apenas uma ordem por nível é permitida até que o sinal oposto ou o módulo de risco feche o trade.

## Gestão de risco
- `UseStopLoss` / `UseTakeProfit` habilitam distâncias de proteção absolutas (expressas em pontos de preço) através do auxiliar integrado `StartProtection`. Stops são opcionais, assim como no código original.
- A estratégia usa ordens de mercado para cada ação e, portanto, respeita o tratamento de slippage configurado globalmente no StockSharp.
- Chamadas de log descrevem cada entrada e saída, tornando mais fácil auditar por que um trade foi executado.

## Parâmetros
- **CciPeriod** – comprimento do Commodity Channel Index.
- **MaLength** – comprimento aplicado a ambas as médias móveis de suavização.
- **HighLevel2 / HighLevel1 / LowLevel1 / LowLevel2** – multiplicadores aplicados ao volume suavizado para criar limites adaptativos.
- **SignalBarOffset** – número de velas fechadas a aguardar antes de agir em uma zona (0 = última vela fechada, 1 = vela anterior, etc.).
- **Smoothing** – tipo de média móvel usado para suavização (subconjunto das opções originais: SMA, EMA, SMMA, WMA, Hull MA, VWMA).
- **AllowLongEntries / AllowShortEntries / AllowLongExits / AllowShortExits** – habilitar ou desabilitar cada lado de forma independente.
- **PrimaryEntryVolume / SecondaryEntryVolume** – volumes para os dois níveis de entrada (usados tanto para trades comprados quanto vendidos).
- **UseStopLoss / StopLossPoints** – stop-loss absoluto opcional.
- **UseTakeProfit / TakeProfitPoints** – take-profit absoluto opcional.
- **CandleType** – período (ou qualquer outro tipo de dado de vela) solicitado ao conector.

## Diferenças da versão MetaTrader
- Apenas métodos de suavização que existem no StockSharp são expostos; filtros exóticos como JJMA, JurX, Parabolic MA, VIDYA e AMA não estão incluídos. Escolha a alternativa disponível mais próxima se precisar de comportamento semelhante.
- O volume da vela é obtido de `ICandleMessage.TotalVolume`. O volume de ticks não é emulado. Se o conector subjacente fornecer apenas contagens de transações, o resultado diferirá do terminal original.
- O gerenciamento de ordens é neteado (posição única) em vez de dois números mágicos independentes. Flags de entrada primária/secundária separados emulam a mesma intenção enquanto permanecem compatíveis com o modelo de execução do StockSharp.
