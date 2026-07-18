# Estratégia PS de Backtester do Barômetro de Janeiro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa o Barômetro de Janeiro, onde uma posição comprada é assumida quando o fechamento de fevereiro a junho supera a máxima de janeiro. Filtros opcionais exigem um resultado positivo do Santa Claus Rally e/ou dos primeiros cinco dias do ano.

## Detalhes

- **Critérios de entrada**: Fechamento de fevereiro a junho acima da máxima de janeiro com filtros sazonais opcionais
- **Comprado/Vendido**: Comprado
- **Critérios de saída**: Encerrar posição em dezembro
- **Stops**: Não
- **Valores padrão**:
  - `CandleType` = 1 month
  - `UseSantaClausRally` = false
  - `UseFirstFiveDays` = false
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Somente comprado
  - Indicadores: Sazonalidade
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Mensal
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
