# Estratégia de Reversão de Curto Prazo em Ações
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Reversão de Curto Prazo em Ações** aplica os princípios de reversão à média em ações. A cada dia, as ações com as maiores perdas na semana anterior são compradas enquanto os vencedores recentes são vendidos a descoberto, apostando em uma reversão de curta duração.

As posições são mantidas por apenas alguns dias antes de serem reavaliadas.

## Detalhes
- **Critérios de entrada**: Classificação diária pelo retorno de uma semana.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Posições fechadas após vários dias ou quando os rankings são atualizados.
- **Stops**: Stop baseado em volatilidade pode ser usado.
- **Valores padrão**:
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Baseados em preço
  - Stops: Sim
  - Complexidade: Básico
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
