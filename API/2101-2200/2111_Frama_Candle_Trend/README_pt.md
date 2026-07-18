# Estratégia FrAMA Candle Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia converte o especialista MetaTrader *Exp_FrAMACandle* em uma estratégia de alto nível do StockSharp.

## Lógica da estratégia

- Usa a **Média Móvel Adaptativa Fractal (FrAMA)** calculada separadamente para os preços de abertura e fechamento das velas.
- Um sinal de alta ocorre quando o FrAMA do preço de fechamento sobe acima do FrAMA do preço de abertura. Se a barra anterior não foi de alta, a estratégia abre uma posição comprada e fecha as vendidas existentes.
- Um sinal de baixa ocorre quando o FrAMA do preço de fechamento cai abaixo do FrAMA do preço de abertura. Se a barra anterior não foi de baixa, a estratégia abre uma posição vendida e fecha as compradas existentes.
- Os sinais são avaliados apenas em velas concluídas. Os valores históricos de cor são armazenados para respeitar o deslocamento `SignalBar`.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `CandleType` | Período usado para o cálculo do indicador. Padrão: 4 horas. |
| `FramaPeriod` | Período do indicador FrAMA. |
| `SignalBar` | Deslocamento da barra usada para detecção de sinais. |
| `BuyOpen` / `SellOpen` | Habilitar abertura de posições compradas/vendidas. |
| `BuyClose` / `SellClose` | Habilitar fechamento de posições compradas/vendidas. |

## Notas

- A estratégia depende exclusivamente de cruzamentos de FrAMA e não implementa gestão de stop-loss ou take-profit.
- O volume da posição é controlado pela propriedade base `Volume` da estratégia.
