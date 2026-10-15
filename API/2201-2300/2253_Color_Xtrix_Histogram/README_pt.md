# Estratégia de Histograma Color XTRIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base nas mudanças de direção de um TRIX suavizado (momentum de média móvel exponencial tripla) calculado a partir de preços de fechamento logarítmicos. Uma posição comprada é aberta quando o histograma TRIX vira para cima após uma queda, enquanto uma posição vendida é aberta quando vira para baixo após uma subida. As posições são revertidas em giros opostos. Não são utilizados stop-loss ou take-profit.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `TRIX rising` && `previous TRIX falling`
  - **Vendido**: `TRIX falling` && `previous TRIX rising`
- **Comprado/Vendido**: Comprado e Vendido
- **Critérios de saída**:
  - Comprado: `TRIX turns downward`
  - Vendido: `TRIX turns upward`
- **Stops**: Não
- **Valores padrão**:
  - `TRIX Length` = 5
  - `Smooth Length` = 5
  - `Momentum Period` = 1
  - `Candle Type` = período 4h
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: TRIX
  - Stops: Não
  - Complexidade: Baixo
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
