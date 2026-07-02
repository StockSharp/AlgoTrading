# Bollinger RSI Estratégia MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia de MA Bollinger RSI transporta os MetaTrader especialistas *BolRSIMAs* para o StockSharp API de alto nível. O sistema combina um
Bollinger Quebra de banda, um filtro RSI e uma média móvel exponencial de período de tempo mais alto (EMA) para identificar negociações de pullback em
a direção da tendência dominante. O dimensionamento automático do lote é preservado: quando habilitado a estratégia converte o risco configurado
fração do patrimônio do portfólio em volume usando o preço atual, a distância de stop Bollinger e o tamanho do contrato do instrumento.

## Lógica de negociação
1. Assine a série de velas primárias (padrão: 1 hora) e calcule Bollinger bandas e RSI no mesmo período de tempo.
2. Assine velas diárias e insira seus preços de fechamento em um EMA período de 200 para reproduzir o filtro de período mais alto usado
no original EA.
3. Gere uma configuração **longa** quando a última vela fechar abaixo da banda inferior, o valor RSI estiver abaixo do limite de sobrevenda
e o fechamento permanece acima do EMA diário. Uma configuração **curta** é acionada por um fechamento acima da banda superior, RSI acima do
limite de sobrecompra e preço abaixo do EMA diário.
4. Abra posições apenas quando nenhuma exposição estiver ativa. Cada novo comércio armazena níveis de stop-loss e take-profit derivados do
valores anteriores de Bollinger: os comprados usam `lowerBand - StopLossOffset` e têm como alvo a banda intermediária; uso de shorts
`upperBand + StopLossOffset` e segmentar a banda intermediária também.
5. Em cada vela finalizada, a estratégia verifica os extremos da vela em relação aos níveis de proteção. Se o baixo/alto tocar o
stop ou target, a posição é fechada imediatamente, emulando as ordens de proteção colocadas pela versão MetaTrader.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Velas de 1 hora | Período primário processado por Bollinger Bandas e RSI. |
| `DailyCandleType` | Velas de 1 dia | Prazo maior que alimenta o filtro de tendência EMA. |
| `BollingerPeriod` | `20` | Número de velas usadas para construir Bollinger Bandas. |
| `BollingerDeviation` | `2` | Multiplicador de largura de banda. |
| `RsiPeriod` | `13` | RSI comprimento de suavização. |
| `RsiUpperLevel` | `70` | Limite de sobrecompra necessário para negociações curtas. |
| `RsiLowerLevel` | `30` | Limite de sobrevenda necessário para negociações longas. |
| `MaPeriod` | `200` | Duração do prazo superior EMA. |
| `StopLossOffset` | `0.0238` | Buffer extra adicionado fora da banda antes de colocar o stop loss. |
| `UseAutoLot` | `true` | Permite dimensionamento de posição baseado em risco. |
| `RiskPerTrade` | `0.05` | Fração do patrimônio alocado para cada negociação quando o lote automático está ativo. |
| `FixedVolume` | `0.1` | Tamanho do pedido quando o dimensionamento automático do lote está desativado. |

## Gestão de dinheiro
- Quando `UseAutoLot` é `true`, o volume é igual a `(equity * RiskPerTrade) / (StopLossOffset * price * contractSize)` arredondado para o
limites de câmbio. Isso reflete a rotina autolot MetaTrader, que divide o valor do risco pela distância do stop em dinheiro e
o tamanho do contrato.
- Se as informações sobre o patrimônio ou o preço não estiverem disponíveis, a estratégia volta para `FixedVolume`, respeitando ainda o
restrições de volume do instrumento.

## Diferenças do especialista MetaTrader
- As ordens stop-loss e take-profit são simuladas por meio de máximos e mínimos de velas, em vez de ordens do lado do servidor, correspondendo ao
resultado do EA original sem depender do envio síncrono de pedidos.
- O filtro EMA usa assinaturas de velas de StockSharp; não há dependência de chamadas de dados diárias específicas de MetaTrader.
- O dimensionamento de risco respeita StockSharp limites de segurança (`MinVolume`, `MaxVolume`, `VolumeStep`) para evitar pedidos rejeitados em exchanges.

## Dicas de uso
- Ajuste `StopLossOffset` ao negociar símbolos com diferentes escalas de preços para que a distância reflita os EA originais
Buffer de 2,38% além da banda Bollinger.
- Se o instrumento usar um período diário diferente (por exemplo, trocas de criptografia), altere `DailyCandleType` adequadamente para que o EMA
reflete o filtro de tendência pretendido.
- Combine a estratégia com trailing stops externos se preferir saídas dinâmicas assim que a meta da banda intermediária for atingida.
