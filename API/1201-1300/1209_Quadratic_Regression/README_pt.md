# Estratégia de Regressão Quadrática
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula uma linha de regressão quadrática para as últimas `Length` barras e opera nos cruzamentos do preço com a linha de regressão.

## Detalhes

- **Critérios de entrada**: O preço cruza acima/abaixo da linha de regressão quadrática.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Cruzamento oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 54.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Quadratic Regression
  - Stops: Não
  - Complexidade: Básico
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
