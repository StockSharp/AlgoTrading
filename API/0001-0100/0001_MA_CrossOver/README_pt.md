# Estratégia de cruzamento de médias móveis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia acompanha a relação entre uma média móvel exponencial rápida e outra lenta. Um cruzamento de alta abre ou reverte para uma posição comprada, enquanto um cruzamento de baixa abre ou reverte para uma posição vendida. Os sinais são avaliados apenas em candles concluídos.

## Detalhes

- **Entrada comprada**: a EMA rápida cruza acima da EMA lenta.
- **Entrada vendida**: a EMA rápida cruza abaixo da EMA lenta.
- **Saída**: um cruzamento oposto reverte a posição; um stop-loss percentual pode fechá-la antes.
- **Valores padrão**:
  - `FastLength` = 100
  - `SlowLength` = 400
  - `StopLossPercent` = 2
  - `CandleType` = 1 minuto
- **Implementações**: C# e Python.
