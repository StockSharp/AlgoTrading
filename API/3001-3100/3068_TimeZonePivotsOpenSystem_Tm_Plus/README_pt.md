# Estratégia Exp Sistema Aberto de Pivôs de Fuso Horário Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port de alto nível do StockSharp do expert advisor **Exp_TimeZonePivotsOpenSystem_Tm_Plus**. Recria o indicador proprietário *TimeZonePivotsOpenSystem* que projeta duas zonas de rompimento em torno da abertura da sessão diária e opera os recuos que seguem um rompimento. Cada componente do script original—atraso de sinal, filtro de tempo, lógica de saída assimétrica e os presets de gestão de dinheiro—foi mapeado para parâmetros explícitos para que o comportamento permaneça consistente com a implementação MQL5.

## Lógica de trading

1. No `StartHour` configurado, a estratégia registra o preço de abertura da sessão. Dois níveis dinâmicos são então traçados a `OffsetPoints` (em pontos) acima e abaixo dessa âncora.
2. Sempre que uma vela finalizada fecha **acima** do nível superior, a estratégia:
   - Agenda uma entrada comprada para ser executada na próxima vela (respeitando o atraso `SignalBar`) somente se a barra atual não está mais acima da faixa.
   - Fecha qualquer posição vendida aberta imediatamente se `SellPosClose` estiver habilitado.
3. Sempre que uma vela finalizada fecha **abaixo** do nível inferior, a estratégia:
   - Agenda uma entrada vendida para a próxima vela desde que a barra atual não esteja mais abaixo da faixa.
   - Fecha qualquer posição comprada aberta imediatamente se `BuyPosClose` estiver habilitado.
4. As entradas são executadas na primeira atualização da próxima vela graças ao `TryExecutePendingEntries`. Isso corresponde ao expert original que atrasa a ordem até que a nova barra comece.

O parâmetro de atraso de sinal `SignalBar` reproduz o deslocamento original `CopyBuffer`. Um valor de `0` reage à barra fechada mais recente, enquanto `1` espera uma barra extra antes de agir, dando confirmação adicional.

## Gerenciamento de ordens

* **Stop-loss / take-profit** – As distâncias são definidas em pontos (`StopLossPoints`, `TakeProfitPoints`) e convertidas em preço usando o passo do instrumento. Ambos os níveis são monitorados usando os extremos das velas para que toques intrabarra acionem uma saída.
* **Saída baseada em tempo** – Quando `TimeTrade` é verdadeiro, a posição é forçosamente fechada após `HoldingMinutes` minutos, espelhando o temporizador `nTime` do código MQL5.
* **Fechamentos manuais** – Sinais de rompimento na direção oposta fecham a operação em andamento se o indicador `BuyPosClose` ou `SellPosClose` correspondente estiver habilitado.

## Gestão de dinheiro

O parâmetro `MoneyMode` reproduz a enumeração `MarginMode`:

- `Lot` – volume fixo igual a `MoneyManagement`.
- `Balance` e `FreeMargin` – usam múltiplos de capital da conta ou margem livre (`MoneyManagement * capital / preço`).
- `LossBalance` e `LossFreeMargin` – dimensionamento baseado em risco que divide a fração de capital desejada pela distância do stop.

Se `StopLossPoints` estiver em zero, os modos de risco recorrem graciosamente ao dimensionamento baseado em preço.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `MoneyManagement` | Coeficiente base usado para dimensionar a posição dependendo de `MoneyMode`. | `0.1` |
| `MoneyMode` | Modelo de dimensionamento de posição (`Lot`, `Balance`, `FreeMargin`, `LossBalance`, `LossFreeMargin`). | `Lot` |
| `StopLossPoints` | Distância do stop-loss expressa em pontos a partir do preço de execução. | `1000` |
| `TakeProfitPoints` | Distância do take-profit expressa em pontos a partir do preço de execução. | `2000` |
| `DeviationPoints` | Parâmetro informativo mantido do expert (configuração de slippage em pontos). | `10` |
| `BuyPosOpen` / `SellPosOpen` | Habilitar ou desabilitar entradas compradas e vendidas. | `true` |
| `BuyPosClose` / `SellPosClose` | Permitir que o rompimento oposto feche posições forçosamente. | `true` |
| `TimeTrade` | Habilitar o filtro de tempo máximo de manutenção. | `true` |
| `HoldingMinutes` | Vida útil máxima da posição em minutos. | `720` |
| `OffsetPoints` | Distância das faixas de pivô a partir da abertura da sessão em pontos. | `200` |
| `SignalBar` | Número de barras para atrasar a avaliação de sinal (0 = última barra fechada). | `1` |
| `CandleType` | Período principal usado para calcular o indicador. | `TimeSpan.FromHours(1).TimeFrame()` |
| `StartHour` | Hora do dia (0-23) que define o preço de abertura da sessão. | `0` |

## Notas de uso

- A estratégia assume que o ativo fornece um `PriceStep` válido. Se o instrumento não tiver esses metadados, um fallback de `0.0001` é usado.
- Como as entradas são acionadas na primeira atualização de uma nova vela, o preço real de execução seguirá o mercado naquele momento, assim como o expert, o que pode diferir do preço teórico de abertura em mercados rápidos.
- Para replicar a sobreposição do indicador original, mantenha o período de backtest em H1 ou abaixo, pois o script MQL5 só opera em períodos horários ou menores.
- Defina `SignalBar` como `0` para comportamento mais responsivo ou como `1` (padrão) para esperar uma barra extra após um rompimento.
