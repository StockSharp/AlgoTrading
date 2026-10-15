# Estratégia Avançada de Cruzamento de EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia vai comprado quando uma EMA de curto prazo cruza acima de uma EMA de longo prazo, filtrando sinais com ATR normalizado, força de tendência ADX e uma verificação de direção do SuperTrend. Os níveis de stop-loss e take-profit se adaptam com base na força do USD inferida a partir de uma EMA de 50 períodos.

## Detalhes

- **Critérios de entrada**:
  - EMA curta cruza acima da EMA longa.
  - ATR normalizado acima dos limites dependendo da direção da tendência.
  - SuperTrend confirma mercado altista ou baixista.
- **Critérios de saída**:
  - Cruzamento inverso de EMA ou ADX acima do limite após um período mínimo de manutenção.
  - Stop-loss ou take-profit atingido.
- **Indicadores**: EMA, ATR, ADX, SuperTrend, SMA (volume).
- **Stops**: Stop-loss e take-profit percentuais dinâmicos.
- **Tipo**: Seguidor de tendência.
- **Período**: 30 minutos (padrão).
