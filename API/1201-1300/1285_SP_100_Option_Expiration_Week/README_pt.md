# Estratégia da Semana de Expiração de Opções S&P 100
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compra no início da semana de expiração de opções (a semana que contém a terceira sexta-feira do mês) e fecha a posição nessa terceira sexta-feira.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: abrir uma posição comprada na segunda-feira da semana de expiração de opções.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - fechar a posição comprada na terceira sexta-feira do mês.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Somente comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
