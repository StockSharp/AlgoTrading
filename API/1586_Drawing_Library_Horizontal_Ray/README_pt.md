# Estratégia de Raio Horizontal da Biblioteca de Desenho
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Desenha raios horizontais nos pontos de cruzamento de SMA e opera na direção do cruzamento.

## Detalhes

- **Critérios de entrada**: `SMA20` cruzando `SMA50` para cima para comprado, para baixo para vendido.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `FastLength` = 20
  - `SlowLength` = 50
  - `CandleType` = 5 minutes
- **Filtros**:
  - Categoria: Desenho
  - Direção: Ambos
  - Indicadores: SMA
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
