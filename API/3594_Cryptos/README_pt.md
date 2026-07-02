# Estratégia criptográfica
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Cryptos Strategy** é uma versão StockSharp de alto nível do consultor especialista MetaTrader4 original `cryptos.mq4`. Ele se concentra no par ETH/USD, combinando bandas Bollinger com uma média móvel ponderada linear (LWMA) para capturar rompimentos da compressão de volatilidade. A estratégia rastreia altos e baixos em um número configurável de velas e calcula dinamicamente as metas de lucro como um múltiplo do intervalo detectado.

## Lógica de negociação

1. **Detecção de tendência** – quando o preço de fechamento toca a banda superior Bollinger, a estratégia muda para uma tendência curta, e quando a banda inferior é tocada, ela muda para uma tendência longa. O toque da banda também congela os valores de swing atuais, desativando as atualizações automáticas de altos/baixos.
2. **Condições de entrada** –
   - Abra uma posição curta quando o preço de fechamento cair abaixo do LWMA, a tendência for curta e não houver posição curta ativa.
   - Abra uma posição longa quando o preço de fechamento subir acima do LWMA, o viés for longo e não houver posição longa ativa.
3. **Projeção de alcance** – oscilações máximas e mínimas (automáticas ou congeladas manualmente) definem a distância do LWMA. Esta distância, expressa em ticks, é multiplicada pelo índice de take-profit para calcular as metas de lucro e o tamanho da posição com base no risco.
4. **Controle de risco** – a estratégia define níveis de take-profit e stop-loss por negociação. Para posições compradas, o stop é colocado abaixo do swing low; para shorts, acima do balanço alto. Stops e metas são recalculados para cada entrada e aplicados dentro do ciclo estratégico.
5. **Saídas finais** – se uma posição longa fechar abaixo da banda inferior Bollinger (ou uma posição curta acima da banda superior), a posição aberta será achatada imediatamente, imitando o comportamento final do EA original.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Tipo de dados da série de velas usada para todos os cálculos de indicadores. |
| `BollingerPeriod`, `BollingerWidth` | Multiplicador de comprimento e desvio padrão das bandas Bollinger. |
| `MaPeriod` | Período da média móvel ponderada linear com base nos preços medianos. |
| `LookbackCandles` | Número de velas examinadas para determinar a oscilação automática de alta/baixa. |
| `TakeProfitRatio` | Multiplicador de intervalo usado para metas de lucro ao negociar ETH/USD. |
| `AlternativeTakeProfitRatio` | Multiplicador de alcance aplicado a todos os outros símbolos. |
| `RiskPerTrade` | Quantidade de capital (na moeda de cotação) que a calculadora de volume tenta arriscar em cada negociação. |
| `ValueIndex`, `CryptoValueIndex` | Multiplicadores convertendo risco em volume para símbolos não criptográficos e criptográficos, respectivamente. |
| `MinVolume`, `MaxVolume` | Limites rígidos para o tamanho da posição após o alinhamento para trocar etapas de volume. |
| `MinRangeTicks` | Faixa projetada mínima permitida em ticks para evitar paradas de distância zero. |
| `SpreadPoints` | Substituição manual do spread em ticks (detectado automaticamente a partir do melhor lance/pergunta, se disponível). |
| `GlobalTrend` | Substituição manual de polarização: `1` força uma configuração curta, `2` força uma configuração longa, `0` deixa a estratégia decidir. |
| `AutoHighLow` | Quando habilitado, os pontos de oscilação são recalculados em cada vela; quando desativados, eles ficam congelados até o próximo toque na banda. |
| `ManualBuyTrigger`, `ManualSellTrigger` | Defina como `true` para solicitar uma entrada longa ou curta imediata (redefinida após a execução). |
| `SkipBuys`, `SkipSells` | Desative a abertura de novas posições longas ou curtas. |

## Dimensionamento de posições

A estratégia replica a lógica MT4: `volume = RiskPerTrade / rangeTicks * valueIndex`. O resultado é alinhado a `VolumeStep` e, em seguida, recortado entre `MinVolume`/`MaxVolume` e os limites impostos pela exchange do instrumento.

## Notas de uso

- A estratégia verifica o valor do portfólio no início. Se o saldo for inferior a `RiskPerTrade * 3`, a negociação será desativada e um aviso será registrado, correspondendo à verificação de segurança de EA.
- Os gatilhos manuais e os controles de polarização possibilitam a sincronização com decisões discricionárias durante a negociação ao vivo.
- ETH/USD usa automaticamente `CryptoValueIndex` e `TakeProfitRatio`; outros instrumentos recorrem aos parâmetros alternativos.
- As paradas e os alvos são aplicados dentro do ciclo estratégico, portanto, nenhum módulo de proteção adicional é necessário.
