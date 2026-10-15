# Estratégia Robot Danu
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que compara os níveis rápidos e lentos do ZigZag derivados das máximas e mínimas das velas.
Uma posição vendida é aberta quando o nível ZigZag rápido está acima do lento.
Uma posição comprada é aberta quando o nível ZigZag rápido está abaixo do lento.

## Detalhes
- **Critérios de entrada**: Comparação de pivôs ZigZag rápidos e lentos.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Relação ZigZag oposta.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `FastLength` = 28
  - `SlowLength` = 56
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Highest, Lowest
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
