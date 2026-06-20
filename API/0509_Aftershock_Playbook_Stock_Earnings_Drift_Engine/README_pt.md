# Estratégia Aftershock Playbook
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Aftershock Playbook** opera a deriva pós-resultados com base em surpresas de LPA.

- **Entrada**: Em uma divulgação de resultados, entrar comprado quando a surpresa ≥ `PositiveSurprise` ou vendido quando a surpresa ≤ `NegativeSurprise`. Os sinais podem ser invertidos com `ReverseSignals`.
- **Stop**: Stop ATR opcional (`AtrLength`, `AtrMultiplier`) aplicado a posições vendidas.
- **Saída**: Opcionalmente fechar posições após `HoldDays` dias corridos (`UseTimeExit`).
- **Reentrada**: Após uma saída lucrativa, a estratégia reentra uma vez na mesma direção. Operações com perda bloqueiam novas entradas até a próxima divulgação de resultados.

*É necessária uma fonte de dados de resultados externos.*
