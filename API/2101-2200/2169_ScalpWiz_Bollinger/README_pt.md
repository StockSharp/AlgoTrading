# Estratégia ScalpWiz Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia ScalpWiz Bollinger** é um sistema contra-tendência que usa Bandas de Bollinger para detectar preços estendidos. Quando o preço de fechamento se afasta muito da banda superior ou inferior, a estratégia abre uma posição na direção oposta esperando uma reversão.

Quatro níveis de distância são verificados. Cada nível corresponde a uma força de sinal diferente e multiplica o volume da operação. O tamanho da posição também é escalado por um percentual de risco do valor atual da carteira.

## Parâmetros

- `BandsPeriod` – número de velas usadas para calcular as Bandas de Bollinger.
- `BandsDeviation` – multiplicador de desvio padrão para as bandas.
- `Level1Pips` … `Level4Pips` – distância da banda em pips que aciona um sinal de nível 1–4.
- `StrengthLevel1Multiplier` … `StrengthLevel4Multiplier` – multiplicadores de volume para cada nível.
- `RiskPercent` – percentual do valor da carteira arriscado por sinal.
- `CandleType` – período de velas utilizado para os cálculos.

## Lógica de trading

1. Assinar velas do período selecionado e calcular as Bandas de Bollinger.
2. Em cada vela finalizada:
   - Se o fechamento estiver acima da banda superior por uma distância de nível configurada, abrir uma posição vendida.
   - Se o fechamento estiver abaixo da banda inferior por uma distância de nível configurada, abrir uma posição comprada.
3. O volume é calculado a partir do percentual de risco e do multiplicador de força do sinal.

A estratégia foi inspirada pelo script MQL original `mcb.scalpwiz.9001.mq4`.
