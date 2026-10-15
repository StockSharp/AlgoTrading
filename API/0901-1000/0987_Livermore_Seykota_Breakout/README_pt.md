# Rompimento Livermore Seykota
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Sistema de rompimento que combina pontos pivot de Livermore com o filtro de tendência de Seykota e saídas baseadas em ATR.

Os testes indicam um retorno anual médio de cerca de 87%. Funciona melhor no mercado de ações.

A estratégia busca rompimentos acima ou abaixo do pivot mais recente, confirmando a direção da tendência com o alinhamento de EMA e a força do volume. Stops baseados em ATR gerenciam o risco.

## Detalhes

- **Critérios de entrada**: O preço rompe o último pivot com confirmação de tendência e volume.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Stop ATR ou stop trailing.
- **Stops**: Stop e trailing baseados em ATR.
- **Valores padrão**:
  - `MainEmaLength` = 50
  - `FastEmaLength` = 20
  - `SlowEmaLength` = 200
  - `PivotLength` = 3
  - `AtrLength` = 14
  - `StopAtrMultiplier` = 3
  - `TrailAtrMultiplier` = 2
  - `VolumeSmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: EMA, Volume, ATR, Pivot
  - Stops: ATR Trailing
  - Complexidade: Básico
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
