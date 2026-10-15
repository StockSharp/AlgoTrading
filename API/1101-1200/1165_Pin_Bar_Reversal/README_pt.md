# Estratégia de Reversão Pin Bar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Utiliza velas Pin Bar com filtro de tendência e stops e alvos baseados em ATR. Um Pin Bar de alta acima da SMA abre uma posição comprada, enquanto um de baixa abaixo dela abre uma posição vendida. As entradas são ignoradas quando a volatilidade está muito baixa.

## Detalhes

- **Critérios de entrada**: Pin Bar na direção da tendência com pavio longo, corpo pequeno e ATR acima de `MinAtr`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Stop-loss ou take-profit baseado em ATR.
- **Stops**: Sim, múltiplos de ATR.
- **Valores padrão**:
  - `TrendLength` = 50
  - `MaxBodyPct` = 0.30
  - `MinWickPct` = 0.66
  - `AtrLength` = 14
  - `StopMultiplier` = 1
  - `TakeMultiplier` = 1.5
  - `MinAtr` = 0.0015
  - `CandleType` = 1 hour
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: SMA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
