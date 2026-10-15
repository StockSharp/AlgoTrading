# Tempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que ilustra utilitários de temporização. Compra quando a máxima excede a abertura por um número de ticks durante uma duração especificada.

## Detalhes

- **Critérios de entrada**: A máxima menos a abertura permanece acima do limiar pelos segundos indicados.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**: A condição falha.
- **Stops**: Não.
- **Valores padrão**:
  - `TicksFromOpen` = 0
  - `SecondsCondition` = 20
  - `ResetOnNewBar` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Somente comprado
  - Indicadores: Preço
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (1m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
