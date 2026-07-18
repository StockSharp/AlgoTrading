# Estratégia de Redução do Preço Médio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Redução do Preço Médio compra quando o Índice de Força Relativa (RSI) cai abaixo de um limiar definido. Cada sinal adiciona à posição comprada existente, reduzindo o preço médio de entrada. A estratégia sai quando o preço de fechamento rompe acima da máxima da barra anterior.

## Detalhes

- **Critérios de entrada**:
  - RSI abaixo de `RsiBuyThreshold`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - O preço de fechamento supera a máxima da barra anterior.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `RsiLength` = 10
  - `RsiBuyThreshold` = 33
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
