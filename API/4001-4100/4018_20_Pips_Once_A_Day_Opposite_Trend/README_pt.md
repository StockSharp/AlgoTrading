# TwentyPipsOnceADayEstratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Porta do especialista MetaTrader **20pipsOnceADayOppositeLastNHourTrend** implementada com o StockSharp API de alto nível. A estratégia é negociada uma vez por hora configurada e abre uma posição contrária contra a deriva das últimas `N` velas horárias. O tamanho da posição segue uma escada martingale que aumenta o lote somente quando uma negociação recente termina com perda. A implementação também impõe uma programação diária de negociação, proteção opcional de rastreamento e um período máximo de manutenção.

## Lógica de negociação

1. A estratégia assina velas horárias (configuráveis através de `CandleType`).
2. Quando uma vela fecha e a próxima hora corresponde a `TradingHour`, a estratégia avalia a direção:
   - Compare o preço de fechamento da última hora concluída com o fechamento de `HoursToCheckTrend` horas atrás.
   - Se o mercado cair nesse intervalo, abra uma posição longa (desvaneça a tendência de baixa).
   - Se o mercado subir, abra uma posição curta.
3. Apenas uma posição pode estar ativa por vez (controlada por `MaxOrders`).
4. Cada negociação herda um take-profit fixo e um stop-loss/trailing stop opcional, ambos expressos em pips em relação ao tamanho do pip do instrumento.
5. Se a posição permanecer aberta por mais de `OrderMaxAgeSeconds` ou a próxima hora estiver fora da sessão permitida definida por `TradingDayHours`, a estratégia fecha a negociação à força.

## Gestão de capital

- `FixedVolume` define o lote base. Defina-o como `0` para derivar o lote do valor do portfólio usando `RiskPercent`. O dimensionamento baseado em risco reflete a lógica EA original: `(portfolio value * RiskPercent) / 1000`.
- Depois que o lote base é calculado, ele é limitado pelos limites `VolumeMin/VolumeMax/VolumeStep` do instrumento e `MinVolume` / `MaxVolume` definidos pelo usuário.
- Uma escada martingale aumenta o próximo lote somente se a respectiva negociação histórica fechar com prejuízo:
  - `FirstMultiplier` se aplica quando a negociação mais recente foi perdida.
  - `SecondMultiplier` se aplica quando a última negociação ganhou, mas a anterior perdeu.
  - A cadeia continua até `FifthMultiplier`, correspondendo ao escalonamento original de cinco etapas.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `FixedVolume` | Volume de negociação fixo. Use `0` para ativar o dimensionamento baseado em risco. |
| `MinVolume` / `MaxVolume` | Limites inferior e superior aplicados após o dimensionamento. |
| `RiskPercent` | Porcentagem da carteira convertida em volume quando `FixedVolume` for igual a zero. |
| `MaxOrders` | Número máximo de posições abertas simultaneamente (padrão `1`). |
| `TradingHour` | Hora do dia (0-23) em que novas negociações podem começar. |
| `TradingDayHours` | Horários ou intervalos separados por vírgula (por exemplo, `0-7,13-22`) que permanecem elegíveis para posições abertas. Quando a próxima hora estiver fora deste conjunto, a estratégia será encerrada. |
| `HoursToCheckTrend` | Lookback em velas horárias usadas para comparação contrária. |
| `OrderMaxAgeSeconds` | Tempo máximo de espera em segundos antes de forçar uma saída. |
| `FirstMultiplier` … `FifthMultiplier` | Martingale multiplicadores atribuídos às perdas encontradas nas últimas cinco negociações fechadas. |
| `StopLossPips` | Distância inicial de stop loss em pips. Defina como `0` para desativar. |
| `TrailingStopPips` | Distância de parada final em pips. Defina como `0` para desativar. |
| `TakeProfitPips` | Tire a distância do lucro em pips. |
| `CandleType` | Tipo de vela usado para geração de sinal (o padrão é o período de 1 hora). |

## Controles e Saídas de Risco

- **Take Profit/Stop Loss**: configurado por meio de `TakeProfitPips` e `StopLossPips` com conversão automática para unidades de preço do instrumento.
- **Trailing stop**: Se ativado, o stop é seguido quando a negociação ganha mais do que o número configurado de pips.
- **Saída de tempo limite**: Posições anteriores a `OrderMaxAgeSeconds` são fechadas ao preço de fechamento da vela atual.
- **Filtro de sessão**: as posições são fechadas quando a próxima hora não está incluída em `TradingDayHours`.

## Notas de uso

- A estratégia funciona com qualquer instrumento que forneça velas horárias e um `PriceStep` válido. Quando o instrumento utiliza pips fracionários (3 ou 5 casas decimais), o tamanho do pip é ajustado automaticamente.
- Para replicar o comportamento de MetaTrader, execute a estratégia em um único instrumento com `CandleType` definido para um período de hora em hora e mantenha o padrão `TradingDayHours` (0-23) para permitir a negociação ao longo do dia.
- A escada martingale assume no máximo cinco negociações históricas relevantes. Redefinir a estratégia limpa esse histórico.
- Como a estratégia é negociada na abertura da hora configurada usando dados de velas fechadas, os preenchimentos ocorrem ao preço disponível quando a nova hora começa.

## Arquivos

- `CS/TwentyPipsOnceADayStrategy.cs` – implementação principal de C#.
- `README.md` – Documentação em inglês (este arquivo).
- `README_zh.md` – documentação chinesa.
- `README_ru.md` – Documentação russa.

As portas Python são omitidas intencionalmente para esta conversão.
