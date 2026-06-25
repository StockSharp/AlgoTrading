# Estratégia Exp Rj SlidingRangeRj Digit System Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia StockSharp é um port do consultor especialista MetaTrader `Exp_Rj_SlidingRangeRj_Digit_System_Tm_Plus`. Ela recria a lógica de trading original baseada no indicador de canal personalizado **Rj_SlidingRangeRj_Digit** e preserva as opções configuráveis de gerenciamento de operações. A estratégia monitora candles finalizados em um período configurável, detecta rompimentos além do canal e reage a esses eventos com entradas atrasadas, saídas temporizadas opcionais e gerenciamento de stop/alvo baseado em preço.

## Lógica do indicador

O indicador Rj_SlidingRangeRj_Digit constrói um canal de preço adaptativo usando um processo de média de múltiplas etapas:

1. Para a banda superior, o máximo-máximo dentro de `UpCalcPeriodRange` barras é calculado para cada uma das últimas `UpCalcPeriodRange` janelas deslizantes, deslocadas por `UpCalcPeriodShift` barras. A média dessas máximas é arredondada para a precisão especificada por `UpDigit`.
2. A banda inferior repete a mesma lógica para mínimas usando `DnCalcPeriodRange`, `DnCalcPeriodShift` e `DnDigit`.
3. Um candle é rotulado como rompimento quando seu preço de fechamento está acima da banda superior (cores `2` / `3`) ou abaixo da banda inferior (cores `0` / `1`). Candles dentro do canal produzem uma cor neutra (`4`).

A estratégia transmite candles finalizados, reconstrói as bandas em cada atualização e armazena os códigos de cor mais recentes para imitar o comportamento `CopyBuffer`/`SignalBar` da implementação MQL.

## Regras de trading

* **Atraso de entrada:** Os sinais são avaliados na barra definida por `SignalBar` (padrão uma barra atrás). A estratégia aguarda até que uma cor de rompimento apareça e a barra anterior não tenha a mesma cor de rompimento. Isso reproduz o atraso original de uma barra antes de tomar uma operação.
* **Entradas compradas:** Habilitadas por `EnableBuyEntries`. Um rompimento altista (`cor 2` ou `3`) aciona uma compra de mercado quando não há posição comprada aberta (a exposição vendida é compensada automaticamente).
* **Entradas vendidas:** Habilitadas por `EnableSellEntries`. Um rompimento baixista (`cor 0` ou `1`) aciona uma venda de mercado quando não há posição vendida aberta.
* **Sinais de saída:**
  * Comprados fecham com cores de rompimento baixista se `EnableBuyExits` for verdadeiro.
  * Vendidos fecham com cores de rompimento altista se `EnableSellExits` for verdadeiro.
  * A saída opcional baseada em tempo (`UseTimeExit`) fecha qualquer posição aberta uma vez que tenha sido mantida por mais de `ExitMinutes`.
  * Níveis opcionais de stop-loss e take-profit expressos em pontos (`StopLossPoints`, `TakeProfitPoints`) são convertidos em deslocamentos de preço usando o `PriceStep` do instrumento.

Todas as ações usam `BuyMarket` / `SellMarket` para que a estratégia reverta automaticamente as posições quando necessário.

## Parâmetros

| Parâmetro | Descrição | Valor padrão |
|-----------|-----------|--------------|
| `CandleType` | Tipo de candle (período) usado para detecção de sinais. | Candles de 8 horas |
| `EnableBuyEntries` / `EnableSellEntries` | Permitir entradas de rompimento compradas/vendidas. | `true` |
| `EnableBuyExits` / `EnableSellExits` | Permitir saídas baseadas em indicador para comprados/vendidos. | `true` |
| `UseTimeExit` | Fechar operações após um tempo de manutenção fixo. | `true` |
| `ExitMinutes` | Limite de tempo de manutenção em minutos. | `1920` |
| `UpCalcPeriodRange`, `UpCalcPeriodShift`, `UpDigit` | Parâmetros da banda do canal superior. | `5`, `0`, `2` |
| `DnCalcPeriodRange`, `DnCalcPeriodShift`, `DnDigit` | Parâmetros da banda do canal inferior. | `5`, `0`, `2` |
| `SignalBar` | Deslocamento de barra usado para avaliar sinais de rompimento. | `1` |
| `StopLossPoints`, `TakeProfitPoints` | Stop-loss / take-profit em pontos de preço (convertidos com `PriceStep`). | `1000`, `2000` |

Defina a propriedade `Volume` da estratégia para controlar o dimensionamento de posição. Os parâmetros de stop-loss e take-profit são opcionais; defina-os como `0` para desabilitar qualquer nível de proteção.

## Notas

* A estratégia espera histórico suficiente para formar o canal deslizante (aproximadamente `max(shift + 2 × range)` candles). Gerencia automaticamente os buffers internos e ignora sinais até que dados suficientes estejam disponíveis.
* O arredondamento de preço é realizado usando dígitos decimais, refletindo o comportamento de arredondamento do indicador MQL.
* A implementação Python é omitida intencionalmente conforme as instruções do projeto; apenas a versão C# é fornecida.
