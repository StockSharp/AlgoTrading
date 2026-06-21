# Estratégia do Fator Semanal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa o padrão de Fator Semanal descrito por Andrea Unger. A estratégia opera rompimentos da máxima ou mínima da sessão quando o intervalo de cinco dias mostra compressão.

## Detalhes
- **Critérios de entrada**: Após o início da sessão, se a condição do Fator Semanal for verdadeira e o preço romper a máxima da sessão -> comprado; romper a mínima da sessão -> vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Fechar em nova sessão ou após dois dias com posição lucrativa.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `RangeFilter` = 0.5
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Weekly factor
  - Stops: Não
  - Complexidade: Médio
  - Período: 15m
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
