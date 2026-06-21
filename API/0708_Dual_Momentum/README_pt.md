# Estratégia de Momentum Duplo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Alterna entre um ativo de risco e um ativo seguro usando momentum duplo.
A estratégia investe no ativo de risco somente quando seu momentum é positivo e maior que o momentum do ativo seguro.

## Detalhes

- **Critérios de entrada**: Momentum do ativo de risco > 0 e > momentum do ativo seguro
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: Mudar para o ativo seguro quando a condição falha
- **Stops**: Não
- **Valores padrão**:
  - `Period` = 12
  - `CandleType` = TimeSpan.FromDays(30).TimeFrame()
- **Filtros**:
  - Categoria: Momentum
  - Direção: Somente comprado
  - Indicadores: RateOfChange
  - Stops: Não
  - Complexidade: Básico
  - Período: Mensal
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
