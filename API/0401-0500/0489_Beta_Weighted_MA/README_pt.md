# Estratégia de MA Ponderada por Beta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Beta Weighted MA (BWMA) utiliza uma distribuição Beta para ponderar os preços recentes, produzindo uma média móvel cujo atraso e suavidade podem ser ajustados com os parâmetros alpha e beta. A estratégia entra em uma posição comprada quando o preço cruza acima da BWMA e em uma posição vendida quando o preço cruza abaixo.

## Detalhes

- **Critérios de entrada**:
  - O preço cruza acima da Beta Weighted Moving Average → entrar comprado.
  - O preço cruza abaixo da Beta Weighted Moving Average → entrar vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - O cruzamento oposto fecha a posição atual e abre a inversa.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 50
  - `Alpha` = 3
  - `Beta` = 3
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado/Vendido
  - Indicadores: Beta Weighted Moving Average
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
