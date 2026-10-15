# Estratégia VR-ZVER v2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia VR-ZVER v2 é um port do StockSharp do clássico assessor especialista do MetaTrader. Ela mantém a ideia de tripla confirmação do script original: cada operação deve ser suportada por médias móveis, o oscilador estocástico e RSI. Apenas quando todos os filtros habilitados concordam, a estratégia coloca uma ordem a mercado.

## Lógica de trading

- Os sinais são avaliados quando uma vela fecha. As flutuações intrábarra são usadas apenas para validar stops ou alvos.
- Três médias móveis exponenciais (rápida, lenta, muito lenta) devem estar empilhadas na mesma ordem para validar a tendência quando o filtro de MA está habilitado.
- O filtro estocástico aguarda um cruzamento de %K/%D perto de bandas superior e inferior configuráveis.
- O filtro RSI exige que o oscilador saia de uma zona neutra (abaixo da banda inferior para comprados, acima da banda superior para vendidos).
- Um sinal é aceito apenas quando todos os filtros habilitados votam na mesma direção. Se algum filtro discorda, nada é operado.
- A estratégia abre uma posição por vez. Não faz hedge nem constrói grids; quando plana, aguarda o próximo sinal alinhado.

## Gestão de posição

- Um take-profit e stop-loss são expressos em pips. O stop inicial é definido em dois terços da distância configurada, reproduzindo o comportamento original do EA.
- Um gatilho de ponto de equilíbrio (também em pips) move o stop para o preço de entrada uma vez que a operação ganhou a distância especificada.
- Trailing stops usam uma distância e um passo adicional. O passo evita que o stop seja atualizado em cada pequena subida e corresponde à lógica de trailing do MT5.
- Operações compradas e vendidas compartilham as mesmas regras de gestão e reagem simetricamente às máximas/mínimas da vela.

## Dimensionamento de posição

- `FixedVolume` maior que zero abre cada ordem com um tamanho fixo.
- Quando `FixedVolume` é definido como zero, a estratégia calcula o volume a partir de `RiskPercent`, o valor atual do portfólio e a distância do stop. O passo de preço e o preço de passo são usados para converter a distância em pips em risco monetário.
- Os volumes são arredondados para respeitar as restrições de `VolumeMin`, `VolumeMax` e `VolumeStep` do instrumento. As ordens são ignoradas se o tamanho calculado for muito pequeno.

## Parâmetros

| Nome | Descrição |
| ---- | --------- |
| `CandleType` | Período usado para geração de sinais (padrão velas de 15 minutos). |
| `FixedVolume`, `RiskPercent` | Escolher entre dimensionamento fixo ou baseado em risco. |
| `StopLossPips`, `TakeProfitPips` | Distâncias de proteção base em pips. |
| `TrailingStopPips`, `TrailingStepPips`, `BreakevenPips` | Limiares de gestão de operações. |
| `AllowLongs`, `AllowShorts` | Habilitar ou desabilitar direções individuais. |
| `UseMovingAverageFilter`, `FastMaPeriod`, `SlowMaPeriod`, `VerySlowMaPeriod` | Filtro de tendência EMA triplo. |
| `UseStochastic`, `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSmooth`, `StochasticUpperLevel`, `StochasticLowerLevel` | Configurações de confirmação estocástica. |
| `UseRsi`, `RsiPeriod`, `RsiUpperLevel`, `RsiLowerLevel` | Banda de confirmação RSI. |

## Notas

- A conversão de pips emula o EA original: símbolos de cinco e três dígitos multiplicam o passo de preço por dez antes de calcular os valores de pip.
- O port do StockSharp usa apenas ordens a mercado. As funcionalidades de bloqueio e ordens pendentes da versão MetaTrader são intencionalmente omitidas para manter a implementação consistente com a API de alto nível.
- Anexe a estratégia a um gráfico se quiser ver os overlays de EMA, estocástico e RSI; eles são desenhados automaticamente quando uma área de gráfico está disponível.
