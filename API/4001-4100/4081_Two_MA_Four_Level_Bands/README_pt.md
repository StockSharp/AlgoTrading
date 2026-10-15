# Estratégia de duas bandas de quatro níveis MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia recria o MetaTrader consultor especialista `ytg_2MA_4Level`. Ele compara uma média móvel rápida com uma mais lenta e aciona entradas quando a curva rápida cruza a curva lenta diretamente ou dentro de quatro bandas de deslocamento configuráveis. As posições são protegidas por distâncias simétricas de stop-loss e take-profit expressas em pips, assim como na implementação original.

## Lógica de sinal
1. Duas médias móveis são calculadas na série de velas selecionada. Tanto o método de média (SMA, EMA, SMMA, LWMA) quanto o preço aplicado podem ser ajustados independentemente para as linhas rápidas e lentas.
2. Em cada vela finalizada, a estratégia amostra as médias móveis `CalculationBar` barras atrás (padrão `1`) e também uma barra antes. Isso reflete a chamada MetaTrader `iMA(..., shift)` e garante que apenas velas fechadas gerem negociações.
3. Um sinal de **compra** é acionado quando a média rápida cruza acima da lenta ou quando o cruzamento acontece acima/abaixo da média lenta deslocada em `UpperLevel1`, `UpperLevel2`, `LowerLevel1` ou `LowerLevel2` pips.
4. Um sinal de **venda** usa as condições espelhadas com a média rápida cruzando abaixo da linha lenta (e as mesmas quatro bandas de deslocamento).
5. A estratégia só abre uma nova posição de mercado quando nenhuma ordem está ativa e a posição atual é estável, correspondendo ao comportamento de ticket único do especialista MQL.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `TakeProfitPips` | `int` | `130` | Distância de lucro em pips. Defina como `0` para desativar o alvo. |
| `StopLossPips` | `int` | `1000` | Distância de stop-loss em pips. Defina como `0` para desativar a parada de proteção. |
| `TradeVolume` | `decimal` | `1` | Tamanho base do lote enviado com cada pedido (ajustado automaticamente para `VolumeStep`). |
| `CalculationBar` | `int` | `1` | Número de barras usadas como âncora para a comparação MA (MetaTrader `shift`). |
| `FastPeriod` / `SlowPeriod` | `int` | `14` / `180` | Durações dos períodos das médias móveis. |
| `FastMethod` / `SlowMethod` | `MovingAverageMethod` | `Smoothed` | Técnica de cálculo da média: `Simple`, `Exponential`, `Smoothed` ou `LinearWeighted`. |
| `FastPrice` / `SlowPrice` | `CandlePrice` | `Median` | Preço aplicado usado por cada média móvel. |
| `UpperLevel1` / `UpperLevel2` | `int` | `500` / `250` | Deslocamentos positivos (em pips) adicionados ao MA lento para verificações de tolerância. |
| `LowerLevel1` / `LowerLevel2` | `int` | `500` / `250` | Deslocamentos negativos (em pips) subtraídos do MA lento para verificações de tolerância. |
| `CandleType` | `DataType` | `15m` período de tempo | Série de velas nas quais os indicadores operam. |

## Notas de implementação
- As ordens stop-loss e take-profit são emuladas por meio de `StartProtection` com distâncias convertidas de pips em unidades de preço usando o `PriceStep` do instrumento. Cotações FX de cinco dígitos recebem automaticamente o multiplicador MetaTrader estilo `*10`.
- As filas internas armazenam apenas os dados necessários para reproduzir a lógica `shift`; nenhum histórico completo de velas é acumulado.
- Os pedidos são emitidos com `BuyMarket` / `SellMarket` e herdam o volume normalizado para que a IU reflita o tamanho do lote ativo.
- A saída do gráfico reúne a série de velas com as médias móveis e as negociações executadas para uma rápida inspeção visual.
- Todos os comentários in-line estão em inglês para cumprir as diretrizes do projeto.

## Dicas de uso
- Escolha o mesmo intervalo de vela que você usaria em MetaTrader; a série padrão de `15` minutos pode ser alterada via `CandleType`.
- Reduza os níveis de deslocamento para tornar os sinais mais seletivos ou aumente-os para aceitar cruzamentos de “quase acidente” mais amplos.
- Definir `CalculationBar` como `0` faz com que a estratégia reaja à última vela fechada (sem atraso), enquanto valores mais altos movem o gatilho ainda mais para o passado para confirmação adicional.
- Desative as pernas de proteção (`StopLossPips = 0`, `TakeProfitPips = 0`) se as saídas precisarem ser gerenciadas manualmente ou por outro módulo.
