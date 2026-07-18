# Estratégia Vicious Mortgage Rates V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera um índice sintético construído a partir de quatro medidas de volatilidade.
Uma posição comprada é aberta quando a EMA rápida do produto cruza acima da EMA lenta, e uma posição vendida no cruzamento oposto.

## Detalhes

- **Critérios de entrada**: EMA rápida do índice combinado cruza acima da EMA lenta
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: cruzamento oposto
- **Stops**: Não
- **Valores padrão**:
  - `FastLength` = 8
  - `SlowLength` = 21
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
