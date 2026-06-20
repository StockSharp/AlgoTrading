# Estratégia 80-20
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia detecta velas onde o preço fecha nos 20% superiores ou inferiores da sessão. Um sinal de alta ocorre quando o fechamento está dentro do quinto superior e a abertura está dentro do quinto inferior do intervalo. Um sinal de baixa ocorre quando a abertura está dentro do quinto superior e o fechamento está dentro do quinto inferior. A abordagem visa capturar reversões rápidas a partir de fechamentos extremos de velas.

## Detalhes

- **Critérios de entrada**:
  - Fechamento nos 20% superiores e abertura nos 20% inferiores → comprado.
  - Abertura nos 20% superiores e fechamento nos 20% inferiores → vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Um sinal oposto reverte a posição.
- **Stops**: Nenhum.
- **Valores padrão**:
  - Range percent = 0.2.
- **Filtros**:
  - Categoria: Padrão
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
