# Estratégia de Cruzamento Linear
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia calcula uma regressão linear do preço baseada no volume para produzir um preço previsto. Uma posição comprada é aberta quando o preço previsto cruza acima de sua média móvel ponderada e a linha MACD está subindo acima do sinal. Uma posição vendida é aberta quando a linha MACD cai abaixo do sinal e as mínimas recentes estão em declínio.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O preço previsto cruza acima de seu WMA e o MACD está subindo acima do sinal.
  - **Vendido**: O MACD está caindo abaixo do sinal e as mínimas fazem mínimas mais baixas.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Nenhum; as posições são revertidas em sinais opostos.
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 21.
  - `LinearLength` = 9.
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame().
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Linear Regression, WMA, MACD
  - Stops: Não
  - Complexidade: Moderado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
