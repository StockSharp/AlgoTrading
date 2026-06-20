# Estratégia de Valor Book-to-Market
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Book-to-Market Value** demonstra a configuração de parâmetros do universo e a assinatura de velas diárias para o fator book-to-market.
Este exemplo é um marcador de posição e atualmente não contém lógica de negociação.

## Detalhes
- **Critérios de entrada**: Lógica do fator não implementada.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: Nenhum.
- **Stops**: Não.
- **Valores padrão**:
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Fundamental
  - Direção: Ambos
  - Indicadores: Fundamentals
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
