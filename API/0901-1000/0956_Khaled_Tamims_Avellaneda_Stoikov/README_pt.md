# Estratégia Avellaneda-Stoikov de Khaled Tamim
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa o modelo de formação de mercado Avellaneda-Stoikov. A estratégia calcula cotações de compra e venda a partir dos dois últimos fechamentos e coloca ordens de mercado quando o preço se desvia além das margens configuráveis.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `close < bidQuote - M`
  - **Vendido**: `close > askQuote + M`
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Gamma` = 2
  - `Sigma` = 8
  - `T` = 0.0833
  - `K` = 5
  - `M` = 0.5
  - `Fee` = 0
- **Filtros**:
  - Categoria: Formação de mercado
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
