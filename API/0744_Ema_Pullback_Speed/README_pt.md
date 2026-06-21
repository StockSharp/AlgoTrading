# Estratégia EMA de Velocidade de Pullback
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia EMA Pullback Speed usa uma EMA dinâmica que se adapta à aceleração do preço. Uma posição comprada é aberta quando o preço retorna à EMA dinâmica durante uma tendência de alta com uma reversão altista e velocidade ascendente suficiente. Uma posição vendida é aberta nas condições opostas. As saídas usam stop loss baseado em ATR e take profit de percentual fixo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Preço acima da EMA dinâmica, reversão altista, preço retornou à EMA, velocidade positiva, EMA curta acima da EMA longa, velocidade ≥ `LongSpeedMin`.
  - **Vendido**: Preço abaixo da EMA dinâmica, reversão baixista, preço retornou à EMA, velocidade negativa, EMA curta abaixo da EMA longa, velocidade ≤ `ShortSpeedMax`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Stop loss ATR e take profit de percentual fixo.
- **Stops**: Stop loss `AtrMultiplier`×ATR, take profit `FixedTpPct`%.
- **Valores padrão**:
  - `MaxLength` = 50
  - `AccelMultiplier` = 3
  - `ReturnThreshold` = 5
  - `AtrLength` = 14
  - `AtrMultiplier` = 4
  - `FixedTpPct` = 1.5
  - `ShortEmaLength` = 21
  - `LongEmaLength` = 50
  - `LongSpeedMin` = 1000
  - `ShortSpeedMax` = -1000
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA, ATR
  - Stops: Stop loss ATR, take profit fixo
  - Complexidade: Médio
  - Período: 5m
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
