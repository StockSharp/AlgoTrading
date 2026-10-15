# Estratégia de Reversão VIX II de Larry Conners
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia opera com base no RSI do índice VIX. Uma posição comprada é aberta quando o RSI do VIX cruza acima do nível de sobrecompra. Uma posição vendida é aberta quando o RSI cruza abaixo do nível de sobrevenda. As posições são fechadas após serem mantidas por um número mínimo de dias.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: RSI(VIX) cruza acima do `Overbought level`.
  - **Vendido**: RSI(VIX) cruza abaixo do `Oversold level`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Fechar posição após `Min holding days` a `Max holding days`.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `RSI period` = 25
  - `Overbought level` = 61
  - `Oversold level` = 42
  - `Min holding days` = 7
  - `Max holding days` = 12
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
