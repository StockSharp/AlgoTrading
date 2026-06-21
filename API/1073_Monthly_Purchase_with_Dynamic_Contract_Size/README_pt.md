# Estratégia de Compra Mensal com Tamanho de Contrato Dinâmico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Compra um número dinâmico de contratos em um dia escolhido de cada mês usando uma porcentagem fixa do capital da conta. O drawdown é acompanhado para fins informativos.

## Detalhes

- **Critérios de entrada**: tempo >= StartDate E dia do mês = BuyDay
- **Comprado/Vendido**: Somente comprado
- **Critérios de saída**: nenhum
- **Stops**: nenhum
- **Valores padrão**:
  - `CandleType` = 1 dia
  - `StartDate` = 2010-01-01
  - `PercentOfEquity` = 0.03
  - `BuyDay` = 1
- **Filtros**:
  - Categoria: Custo médio em dólar
  - Direção: Comprado
  - Indicadores: Não
  - Stops: Não
  - Complexidade: Iniciante
  - Período: Longo prazo
  - Sazonalidade: Mensal
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
