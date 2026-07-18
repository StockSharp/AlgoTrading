# Estratégia Três Vermelhas / Três Verdes com Filtro ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Entra comprado após três velas de baixa consecutivas se o ATR estiver acima de sua SMA de 30 períodos. Sai após três velas de alta ou quando a duração máxima da operação é atingida.

## Parâmetros

- **CandleType**: Tipo de velas.
- **MaxTradeDuration**: Número máximo de barras para manter uma posição aberta.
- **UseGreenExit**: Se deve sair após três velas verdes.
- **AtrPeriod**: Período para o cálculo do ATR (0 desativa o filtro).
