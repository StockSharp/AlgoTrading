# Estratégia Extrema Sweet Spot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sweet Spot Extreme é uma porta direta do MetaTrader 4 consultor especialista "Sweet_Spot_Extreme.mq4" construído no StockSharp de alto nível API. A estratégia busca fortes retrocessos dentro de uma tendência existente, combinando duas médias móveis exponenciais em velas de 15 minutos com um filtro de índice de canal de commodities de 30 minutos (CCI). O dimensionamento da posição reflete os controles de risco originais, incluindo a redução de lote no estilo MetaTrader após sequências de derrotas.

## Lógica central

1. **Confirmação da inclinação da tendência.** O EMA principal (`MaPeriod`, padrão 85) e o próximo EMA (`CloseMaPeriod`, padrão 70) são alimentados com preços medianos de 15 minutos. Uma configuração longa exige que ambos os EMAs se inclinem para cima; uma configuração curta precisa que ambos se inclinem para baixo.
2. **CCI filtro de exaustão.** Uma segunda assinatura de vela (30 minutos por padrão) alimenta o `CciPeriod` CCI. As negociações longas só disparam quando CCI cai abaixo de `BuyCciLevel` (-200), enquanto as posições curtas exigem CCI acima de `SellCciLevel` (+200).
3. **Limite da pirâmide.** A posição líquida agregada não pode exceder `MaxTradesPerSymbol × volume`. Quando um novo sinal aparece, a estratégia fecha qualquer exposição oposta e então soma a capacidade permitida na direção do sinal.
4. **Saídas.** As posições são fechadas quando a tendência EMA perde sua vantagem de inclinação (espelhando a condição MQL `MA <= MAprevious`) ou depois que o preço percorre `StopPoints` o instrumento aponta a favor da posição.

## Gestão de risco

- **Volume baseado em risco.** O tamanho padrão do pedido é `Portfolio.CurrentValue × MaximumRisk ÷ price`. Quando faltam informações de patrimônio, o mecanismo recorre ao parâmetro `Lots` (ou à estratégia `Volume`).
- **Ajuste de sequência de perdas.** Após duas ou mais negociações consecutivas perdidas, o tamanho do novo pedido é reduzido em `volume × losses ÷ DecreaseFactor`, correspondendo ao auxiliar MQL `LotsOptimized()`.
- **Normalização.** O volume final é alinhado com o `VolumeStep` do instrumento, delimitado por `MinVolume` e cortado por `Security.MaxVolume` quando fornecido.

## Parâmetros

| Nome | Padrão | Descrição |
|------|---------|-------------|
| `MaxTradesPerSymbol` | `3` | Número máximo de entradas agregadas permitidas por direção. |
| `Lots` | `1` | Tamanho de lote fixo substituto quando o patrimônio do portfólio não estiver disponível. |
| `MaximumRisk` | `0.05` | Fração do patrimônio usado para dimensionar cada nova negociação. |
| `DecreaseFactor` | `6` | Divisor que reduz o próximo pedido após perdas consecutivas. |
| `StopPoints` | `10` | Distância alvo de lucro em pontos de instrumento. Defina como `0` para desativar. |
| `MaPeriod` | `85` | Período EMA aplicado a velas de 15 minutos para a verificação da inclinação da tendência. |
| `CloseMaPeriod` | `70` | Período EMA aplicado a velas de 15 minutos para o filtro de suavização próximo. |
| `CciPeriod` | `12` | Lookback usado para o filtro CCI de 30 minutos. |
| `BuyCciLevel` | `-200` | Limite de sobrevenda CCI necessário para entradas longas. |
| `SellCciLevel` | `200` | Limite de sobrecompra de CCI necessário para entradas curtas. |
| `MinVolume` | `0.1` | Volume mínimo permitido após a normalização. |
| `TrendCandleType` | `15m` | Tipo de vela usado para cálculos de EMA (preço médio). |
| `CciCandleType` | `30m` | Tipo de vela usado para o filtro CCI. |

## Notas e limitações

- StockSharp opera no modo de compensação, portanto, vários tickets MT4 são representados como uma única posição agregada. A proteção `MaxTradesPerSymbol`, portanto, limita a exposição líquida em vez de contar pedidos individuais.
- O EA original dependia de `AccountFreeMargin` para dimensionamento. Esta porta se aproxima de `Portfolio.CurrentValue`; ajuste `MaximumRisk` ou `Lots` para atender às especificações do contrato do seu corretor.
- Certifique-se de que ambas as assinaturas de vela estejam habilitadas na fonte de dados, caso contrário, os filtros EMA ou CCI nunca serão formados e a estratégia permanecerá inativa.
