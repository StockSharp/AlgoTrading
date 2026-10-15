# Estratégia Simples XAUUSD com 20 de Lucro e 100 de Perda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia abre uma posição comprada quando não há posição aberta e ambos os temporizadores de resfriamento estão inativos.
Fecha a posição quando o lucro não realizado atinge $20 ou a perda chega a $100.
Após uma saída lucrativa, a estratégia aguarda 15 minutos antes de reentrar, e após uma saída com perda aguarda 12 horas.

## Parâmetros

- `ProfitTarget` – meta de lucro em USD.
- `LossLimit` – perda máxima em USD.
- `TradeCooldown` – tempo de espera após uma perda.
- `EntryCooldown` – tempo de espera após um lucro.
- `CandleType` – período de tempo das velas.
