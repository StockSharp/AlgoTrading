# Estratégia Ichimoku Barabashkakvn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria o expert advisor Ichimoku de Vladimir Karputov (edição barabashkakvn) sobre a API de alto nível do StockSharp. Combina o clássico cruzamento Tenkan/Kijun com a confirmação da nuvem Kumo e adiciona uma gestão de risco detalhada idêntica ao original MetaTrader.

## Como funciona

- **Pilha de indicadores** – um único indicador Ichimoku Kinko Hyo fornece os valores de Tenkan-sen, Kijun-sen, Senkou Span A e Senkou Span B. Os períodos padrão permanecem 9/26/52.
- **Entradas compradas** – acionadas quando Tenkan cruza para cima através de Kijun e o preço de fechamento está acima de Senkou Span B. A detecção do cruzamento usa o valor anterior de Tenkan, espelhando a lógica barra por barra do EA.
- **Entradas vendidas** – aparecem quando Tenkan cruza para baixo através de Kijun enquanto o fechamento está abaixo de Senkou Span A.
- **Gestão de posição** – apenas uma posição líquida é mantida. Sinais opostos fecham as operações existentes primeiro, reproduzindo o fluxo de reversão em dois passos do script.
- **Janela de trading** – um filtro de horas opcional permite ao sistema operar apenas entre as horas de início/fim configuradas (inclusive) usando a mesma comparação que a versão MQL.

## Gestão de risco

- **Stops e alvos direcionais** – posições compradas e vendidas usam distâncias independentes de stop-loss/take-profit em pips. Os pips são convertidos em unidades de preço usando o tamanho do passo do instrumento com um ajuste de ×10 para cotações de 3 e 5 decimais, correspondendo ao tratamento de pontos do EA.
- **Trailing stop** – cada direção tem sua própria distância de trailing mais um passo de trailing comum. O stop avança apenas depois que o movimento supera `(distância de trailing + passo de trailing)`, exatamente como no código original.
- **Execução de proteção** – as verificações de stop-loss e take-profit ocorrem em cada vela concluída para que os níveis de proteção virtuais se comportem como ordens gerenciadas pelo broker do MetaTrader.

## Parâmetros

- `TenkanPeriod` *(padrão 9)* – comprimento do Tenkan-sen.
- `KijunPeriod` *(padrão 26)* – comprimento do Kijun-sen.
- `SenkouSpanBPeriod` *(padrão 52)* – comprimento do Senkou Span B.
- `CandleType` *(padrão velas de 1 hora)* – fonte de dados para cálculos.
- `OrderVolume` *(padrão 1 lote)* – tamanho da operação.
- `BuyStopLossPips` / `SellStopLossPips` *(padrão 100)* – distâncias de stop-loss em pips.
- `BuyTakeProfitPips` / `SellTakeProfitPips` *(padrão 300)* – distâncias de take-profit em pips.
- `BuyTrailingStopPips` / `SellTrailingStopPips` *(padrão 50)* – distâncias de trailing em pips.
- `TrailingStepPips` *(padrão 5)* – incremento mínimo de lucro necessário para deslocar o trailing stop.
- `UseTradeHours` *(padrão false)* – habilitar o filtro de sessão.
- `StartHour` / `EndHour` *(padrões 0/23)* – limites inclusivos da janela de trading (0–23).

Esses padrões correspondem ao EA publicado. Todos os parâmetros são expostos através de objetos `StrategyParam<T>`, para que possam ser otimizados ou ajustados dentro do StockSharp Designer sem tocar no código fonte.
