# Estratégia de Compra/Venda Baseada em Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera tijolos Renko criados com tamanho baseado em ATR. Uma posição comprada é aberta quando o fechamento do Renko cruza acima da sua abertura. Uma posição vendida é aberta quando o fechamento cruza abaixo da abertura.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Fechamento cruza acima da abertura.
  - **Vendido**: Fechamento cruza abaixo da abertura.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - Comprimento ATR 10.
- **Filtros**:
  - Categoria: Reversão
  - Direção: Ambos
  - Indicadores: Renko
  - Stops: Não
  - Complexidade: Simples
  - Período: Sem base temporal
