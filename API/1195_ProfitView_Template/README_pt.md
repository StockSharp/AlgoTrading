# Modelo de Estratégia ProfitView
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Uma estratégia básica de cruzamento de médias móveis com tipos de suavização configuráveis, derivada do modelo ProfitView.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: MA1 cruza acima de MA2.
  - **Vendido**: MA1 cruza abaixo de MA2.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `MA1 Type` = SMA, `MA1 Length` = 10
  - `MA2 Type` = SMA, `MA2 Length` = 100
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Médias móveis
  - Stops: Não
  - Complexidade: Básico
