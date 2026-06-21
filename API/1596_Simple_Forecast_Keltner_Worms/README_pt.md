# Estratégia de Previsão Simples - Keltner Worms
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia constrói um canal Keltner dinâmico e opera quando o preço se move fora da banda.

## Detalhes

- **Critérios de entrada**:
  - Preço de fechamento acima do canal superior abre uma posição comprada.
  - Preço de fechamento abaixo do canal inferior abre uma posição vendida.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - O sinal oposto fecha a posição.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 10
- **Filtros**:
  - Categoria: Canal
  - Direção: Ambos
  - Indicadores: EMA, ATR
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
