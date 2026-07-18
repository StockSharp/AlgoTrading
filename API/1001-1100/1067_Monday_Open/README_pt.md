# Estratégia de Abertura de Segunda-feira
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compra no início da semana e fecha a posição no fechamento de terça-feira dentro de um intervalo de anos especificado.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: abrir uma posição comprada na segunda-feira.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - fechar a posição comprada na terça-feira.
- **Stops**: Não.
- **Valores padrão**:
  - `StartYear` = 2023.
  - `EndYear` = 2025.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoria: Sazonalidade
  - Direção: Comprado
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
