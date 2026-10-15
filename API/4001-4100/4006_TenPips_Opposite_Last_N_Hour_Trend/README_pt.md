# Dez Pips Opostos à Estratégia de Tendência da Última N Hora
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma versão fiel do especialista MetaTrader **10pipsOnceADayOppositeLastNHourTrend**. Ele negocia exatamente uma vez por dia em uma hora configurável e deliberadamente assume o lado oposto da mudança de preço observada nas últimas *N* velas horárias concluídas. A lógica foi projetada para pares de moedas com preços de cinco dígitos, mas a versão C# adapta automaticamente o tamanho do pip usando o `PriceStep` do instrumento e o número de decimais.

No horário de negociação selecionado, a estratégia inspeciona o preço de fechamento de `HoursToCheckTrend` horas atrás e o compara com o fechamento da vela horária concluída mais recente:

- Se o fechamento mais antigo for **mais alto**, o mercado está caindo (baixa), então a estratégia abre uma posição **longa**.
- Caso contrário, o mercado tem subido (alta), portanto abre uma posição **curta**.

As posições são fechadas por paradas de proteção, saída diária baseada no tempo ou manualmente quando o mercado está fora da janela de negociação.

## Gestão de dinheiro

O dimensionamento da posição reflete a escada martingale do especialista original:

1. O volume base vem de `FixedVolume`. Quando definida como zero, a estratégia volta ao dimensionamento baseado em risco usando `Portfolio.CurrentValue * MaximumRisk / 1000` arredondado para uma casa decimal.
2. O volume é limitado por `MinimumVolume`, `MaximumVolume`, os limites de volume do instrumento e um soft cap igual a `Portfolio.CurrentValue / 1000` lotes.
3. Após cada negociação fechada o resultado é armazenado (até as últimas cinco negociações). Ao preparar uma nova entrada, a estratégia verifica esse histórico e multiplica o tamanho do lote de acordo com a primeira perda que encontra, usando a sequência `FirstMultiplier`… `FifthMultiplier`. Isso reproduz as verificações `OrderSelect` aninhadas da versão MQL.

## Risk controls

- `StopLossPips`, `TakeProfitPips` e `TrailingStopPips` funcionam em unidades pip. A porta recalcula o tamanho do pip com o multiplicador decimal padrão de 3/5 para símbolos Forex.
- Os trailing stops são simétricos para posições longas e curtas. No código MQL original, a trilha do lado curto nunca foi acionada devido a um erro de sinal; a versão C# corrige isso para que ambas as direções se comportem de forma idêntica.
- `OrderMaxAge` fecha qualquer posição que sobreviva por mais tempo do que a duração configurada (21 horas por padrão).
- Fora do horário de negociação permitido, a estratégia liquida qualquer exposição aberta para permanecer estável até a próxima sessão.
- `MaxOrders` protege contra reentradas acidentais, exigindo que não haja posições abertas ou ordens ativas quando um novo sinal for avaliado.

## Fluxo de trabalho detalhado

1. Assine velas horárias (o período pode ser alterado com `CandleType`).
2. Colete o preço de fechamento de cada vela acabada em um pequeno buffer rolante.
3. Na primeira vela completada na hora permitida:
   - Verifique o estado do portfólio/conexão e confirme que nenhuma posição está aberta.
   - Certifique-se de que temos pelo menos `HoursToCheckTrend` velas históricas para comparar.
   - Determine a direção comparando o fechamento atual com o fechamento há `HoursToCheckTrend` barras.
   - Calcule o tamanho do lote usando a rotina de gerenciamento de dinheiro acima e envie uma ordem de mercado.
4. Enquanto uma posição está aberta a estratégia:
   - Avalia os níveis de stop-loss, take-profit e trailing usando preços máximos/ mínimos de velas.
   - Atualiza o trailing stop após novas máximas (para posições compradas) ou mínimas (para posições vendidas).
   - Rastreia o carimbo de data/hora da entrada para que possa impor `OrderMaxAge`.
   - Registra o lucro/perda realizado quando a negociação é fechada para alimentar os multiplicadores de martingale.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `FixedVolume` | Fixed lot size. Defina como `0` para usar o dimensionamento baseado em risco. | `0.1` |
| `MinimumVolume` | Hard lower bound for the order volume. | `0.1` |
| `MaximumVolume` | Limite superior rígido para o volume do pedido. | `5` |
| `MaximumRisk` | Fração do patrimônio utilizado quando `FixedVolume = 0`. | `0.05` |
| `MaxOrders` | Máximo de ordens/posições simultâneas. | `1` |
| `TradingHour` | Hora do dia (0–23) em que novas negociações são permitidas. | `7` |
| `HoursToCheckTrend` | Janela retrospectiva em horas para comparação de tendências. | `30` |
| `OrderMaxAge` | Vida útil máxima de uma posição. | `21h` |
| `StopLossPips` | Distância de stop-loss em pips. | `50` |
| `TakeProfitPips` | Distância de lucro em pips. | `10` |
| `TrailingStopPips` | Distância do trailing-stop em pips. | `0` (disabled) |
| `FirstMultiplier` … `FifthMultiplier` | Multiplicadores de lote aplicados quando a negociação perdedora mais recente é encontrada na respectiva profundidade. | `4`, `2`, `5`, `5`, `1` |
| `CandleType` | Prazo para assinatura da vela. | `1 hour` |

## Diferenças do especialista MQL original

- O dimensionamento de Martingale, o vencimento do pedido e a lógica da janela de negociação são reproduzidos um a um. A única mudança deliberada é o stop móvel simétrico no lado curto para corrigir o bug do sinal no script original.
- Todos os níveis de proteção são executados com ordens de mercado na próxima vela concluída porque as estratégias StockSharp não registram ordens stop/limit separadas ao usar ajudantes de alto nível. Isso corresponde ao comportamento do especialista original quando suas ordens stop foram acionadas.
- O patrimônio da conta é lido em `Portfolio.CurrentValue`. Se o adaptador não fornecer este campo, a estratégia retornará à base `Volume` (padrão `1`).
- A lista de horários de negociação permitidos reflete a matriz original de `0…23`. Para restringir a negociação a dias específicos, você pode editar `_tradingDayHours` dentro do construtor.

## Notas de uso

- Funciona melhor em dados Forex horários, onde os cálculos do tamanho do pip usando a heurística `PriceStep` ×10 são válidos.
- Sempre verifique se `Security.VolumeStep`, `VolumeMin` e `VolumeMax` estão definidos pelo conector para que a estratégia possa ajustar os tamanhos dos lotes corretamente.
- Como as entradas são avaliadas apenas uma vez por vela finalizada, a estratégia deve ser lançada antes do horário de negociação escolhido para que o primeiro sinal do dia não seja perdido.
