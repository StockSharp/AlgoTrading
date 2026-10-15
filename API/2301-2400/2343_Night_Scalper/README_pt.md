# Estratégia de Scalping Noturno
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera durante a sessão noturna usando Bandas de Bollinger. Abre posições apenas após uma hora de início especificada quando a largura da banda é estreita e o preço rompe para fora das bandas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: após `Start Hour`, o preço fecha abaixo da Banda de Bollinger inferior e a largura da banda é menor que `Range Threshold`.
  - **Vendido**: após `Start Hour`, o preço fecha acima da Banda de Bollinger superior e a largura da banda é menor que `Range Threshold`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - A posição é fechada se o tempo cair antes de `Start Hour` do dia seguinte.
  - Stop-loss e take-profit protetores gerenciados por `StartProtection`.
- **Stops**: Usa `StartProtection` com offsets fixos de stop-loss e take-profit.
- **Valores padrão**:
  - `BB Period` = 40
  - `BB Deviation` = 1
  - `Range Threshold` = 450
  - `Stop Loss` = 370
  - `Take Profit` = 20
  - `Start Hour` = 19
  - `Candle Type` = 1h
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Curto prazo
