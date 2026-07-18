# Estratégia SMTP de Larry Conners
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia somente comprada que compra após uma mínima de 10 barras quando a barra atual tem o maior range das últimas 10 barras e fecha no 25% superior do seu range. A entrada é colocada um tick acima da máxima; o stop-loss segue as mínimas sucessivas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: a mínima atual é igual à mínima das últimas 10 barras, o range de hoje é o maior das últimas 10 e o fechamento está no 25% superior do range; colocar uma ordem de compra stop em `High + TickSize`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Trailing stop na mínima mais alta desde a entrada.
- **Stops**: Sim.
- **Valores padrão**:
  - `TickSize` = 0.01
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoria: Reversão
  - Direção: Comprado
  - Indicadores: Highest, Lowest
  - Stops: Sim
  - Complexidade: Básico
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
