# Estratégia Coletora de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma conversão do algoritmo MQL original `TrendCollector.mq4`. Combina detecção de tendência usando duas médias móveis exponenciais com filtros de momentum e volatilidade.

## Lógica da estratégia

- **EMA Rápida vs EMA Lenta** – A estratégia segue a tendência principal comparando uma EMA rápida com uma EMA lenta.
- **Oscilador Estocástico** – Determina condições de sobrecompra e sobrevenda. Posições compradas abrem quando o valor estocástico está abaixo do limiar inferior e a EMA rápida está acima da EMA lenta. Posições vendidas são acionadas quando o valor estocástico está acima do limiar superior e a EMA rápida está abaixo da EMA lenta.
- **Filtro de Volatilidade ATR** – As negociações ocorrem apenas quando o valor ATR atual está abaixo do limite de volatilidade, evitando períodos de alta volatilidade.
- **Janela de Negociação** – As ordens são geradas apenas entre as horas de início e fim configuradas.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| FastMaLength | Período da EMA rápida | 4 |
| SlowMaLength | Período da EMA lenta | 204 |
| StochasticPeriod | Período do oscilador estocástico | 14 |
| StochasticUpper | Nível superior do estocástico | 80 |
| StochasticLower | Nível inferior do estocástico | 20 |
| AtrPeriod | Período do ATR | 14 |
| AtrLimit | Valor ATR máximo permitido para negociar | 2 |
| StartHour | Hora de início da janela de negociação | 5 |
| EndHour | Hora de fim da janela de negociação | 24 |
| CandleTimeFrame | Período das velas processadas | 5 minutos |

A versão em Python não está disponível atualmente.
