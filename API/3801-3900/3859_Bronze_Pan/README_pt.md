# Estratégia do Pan Bronze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma versão StockSharp do MetaTrader 4 consultor especialista "Bronzew_pan". Ele negocia um único instrumento em velas finalizadas e combina o oscilador proprietário DayImpuls com Williams %R e o Commodity Channel Index (CCI) para detectar reversões de impulso.

## Como funciona

1. Assina o tipo de vela configurado e executa DayImpuls, Williams %R e CCI com o mesmo período.
2. Mantém contabilidade independente de exposições longas e curtas para emular o comportamento de cobertura original.
3. Fecha todas as posições quando o lucro flutuante atingir `ProfitTarget` ou cair abaixo de `LossTarget`.
4. Abre uma venda quando DayImpuls permanece acima de `DayImpulsShortLevel` e declina, enquanto Williams %R está acima de `WilliamsLevelUp` e CCI excede `CciLevel`.
5. Abre uma compra quando DayImpuls fica abaixo de `DayImpulsLongLevel` e sobe, enquanto Williams %R está abaixo de `WilliamsLevelDown` e CCI é menor que `-CciLevel`.
6. Se o PnL flutuante ultrapassar os limites de `PredBand`, a estratégia envia uma grande ordem média multiplicada por `LotMultiplier` para inverter a direção, espelhando a lógica de recuperação de emergência de MetaTrader.
7. Os valores individuais de stop-loss e take-profit são monitorados para cestas longas e curtas usando distâncias de pip convertidas em preços.
8. Nenhuma nova negociação é aberta quando o saldo da conta cai abaixo de `MinimumBalance` ou quando as cestas longas e curtas estão ativas.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Volume base para entradas. | `0.1` |
| `LongStopLossPips` | Distância de stop-loss para cestas longas em pips. | `0` |
| `ShortStopLossPips` | Distância de stop-loss para cestas curtas em pips. | `0` |
| `LongTakeProfitPips` | Distância de lucro para cestas longas em pips. | `0` |
| `ShortTakeProfitPips` | Distância de lucro para cestas curtas em pips. | `0` |
| `IndicatorPeriod` | Comprimento usado por DayImpuls, Williams %R e CCI. | `14` |
| `CciLevel` | Limite absoluto de CCI confirmando sobrecompra/sobrevenda. | `150` |
| `WilliamsLevelUp` | Williams %R nível necessário para shorts. | `-15` |
| `WilliamsLevelDown` | Williams %R nível necessário para posições compradas. | `-85` |
| `DayImpulsShortLevel` | Nível DayImpuls que permite entradas curtas. | `50` |
| `DayImpulsLongLevel` | Nível DayImpuls que permite entradas longas. | `-50` |
| `ProfitTarget` | Lucro flutuante que fecha todas as posições. | `500` |
| `LossTarget` | Perda flutuante que fecha todas as posições. | `-2000` |
| `PredBand` | Faixa de lucro usada para acionar reversões de média. | `100` |
| `LotMultiplier` | Multiplicador aplicado ao volume base durante reversões. | `30` |
| `MinimumBalance` | Saldo mínimo da conta necessário para continuar negociando. | `3000` |
| `CandleType` | Período usado para assinaturas de velas. | `15m` |

## Notas

- O oscilador DayImpuls replica a suavização dupla EMA original sobre corpos de velas expressos em pontos.
- Os valores de stop-loss e take-profit são opcionais; a configuração `0` desativa o respectivo lado de proteção.
- A estratégia depende de velas acabadas e ignora barras incompletas.
