# Estratégia GM-8 e ADX com Segunda EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia entra em operações quando o preço cruza uma SMA GM-8 e se alinha com uma segunda EMA enquanto o ADX confirma uma tendência forte.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o preço cruza acima da SMA e fecha acima da SMA e da segunda EMA com ADX acima do limiar.
  - **Vendido**: o preço cruza abaixo da SMA e fecha abaixo da SMA e da segunda EMA com ADX acima do limiar.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - **Comprado**: o preço cruza abaixo da SMA.
  - **Vendido**: o preço cruza acima da SMA.
- **Stops**: Usa StartProtection.
- **Valores padrão**:
  - `GM Period` = 15
  - `Second EMA Period` = 59
  - `ADX Period` = 8
  - `ADX Threshold` = 34
  - `Candle Type` = 15m
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: SMA, EMA, ADX
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Curto prazo

