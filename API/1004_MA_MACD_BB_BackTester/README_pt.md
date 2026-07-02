# Estratégia MA MACD BB BackTester
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina três indicadores selecionáveis: cruzamento de média móvel simples, cruzamento de MACD ou rompimento de Bandas de Bollinger. Apenas um modo de indicador fica ativo por vez, e a direção da operação pode ser comprada ou vendida.

## Parâmetros
- `CandleType` — período de tempo das velas.
- `Indicator` — indicador a utilizar (MA, MACD, BB).
- `Direction` — direção da operação (Long ou Short).
- `MaLength` — período da média móvel.
- `FastLength` — comprimento da EMA rápida do MACD.
- `SlowLength` — comprimento da EMA lenta do MACD.
- `SignalLength` — comprimento do sinal do MACD.
- `BbLength` — período das Bandas de Bollinger.
- `BbMultiplier` — multiplicador das Bandas de Bollinger.
- `StartDate` — data de início.
- `EndDate` — data de fim.
