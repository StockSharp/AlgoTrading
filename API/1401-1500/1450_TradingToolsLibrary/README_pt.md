# Estratégia de Biblioteca de Ferramentas de Trading
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia simples de cruzamento de SMA com filtro RSI e tempo de resfriamento entre entradas.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: SMA rápida cruza acima da SMA lenta e RSI abaixo de `RsiUpper`
  - **Vendido**: SMA rápida cruza abaixo da SMA lenta e RSI acima de `RsiLower`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Sinal inverso
- **Stops**: Nenhum
- **Valores padrão**:
  - `ShortLength` = 10
  - `LongLength` = 30
  - `RsiLength` = 14
  - `CooldownBars` = 3
  - `RsiUpper` = 60
  - `RsiLower` = 40
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: SMA, RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
