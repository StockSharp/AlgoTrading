# Estratégia de Limites de Preço de Futuros de Ações CME
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula os níveis de limite de preço diários para futuros de ações CME. Captura um preço de referência em uma hora especificada e calcula os limites de alta/baixa (+/-5%), bem como os níveis de limite de baixa de -7%, -13% e -20%. Os resultados são gravados no log para monitoramento.

## Parâmetros

- **ManualReference** – preço de referência manual (0 para desabilitar).
- **ShowLimitDownLevels** – habilitar o registro dos níveis -7/-13/-20%.
- **OffsetHour** – hora (0-23) para capturar o preço de referência.
- **CandleType** – tipo de candle a processar (padrão: 1 minuto).
