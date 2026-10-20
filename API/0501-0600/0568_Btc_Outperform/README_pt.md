# BTC Outperform

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Regras de negociação

Agrupa velas diárias UTC concluídas em períodos fixos. Os semanais são ancorados na segunda-feira 1970-01-05 e os de 90 dias em 1970-01-01. Só publica o fechamento quando todas as velas diárias do período estão disponíveis. Períodos iniciais incompletos ou com lacunas não publicam fechamento.

Compara os últimos fechamentos completos observados: semanal maior compra, menor vende e igualdade mantém a posição. Partindo de zero, envia `Volume`; para inverter, usa `Volume+abs(Position)`, deixando a posição oposta completa. Períodos em formação não alteram a comparação. Ordens pendentes impedem duplicações. Sem stop de preço ou alvo automático.

## Parâmetros

| Parâmetro | Padrão |
|---|---|
| `CandleType` | `1d` |
| `WeeklyDays` | `7` |
| `QuarterlyDays` | `90` |
