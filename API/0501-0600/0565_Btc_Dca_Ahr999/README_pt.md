# Estratégia BTC DCA AHR999
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia compra Bitcoin toda segunda-feira entre as datas de início e fim configuradas. O valor investido depende do índice AHR999, que combina uma média geométrica do preço com um modelo de crescimento logarítmico do Bitcoin.

## Detalhes

- **Critérios de entrada**:
  - Nas segundas-feiras dentro do intervalo de datas, se AHR999 < 0.45, comprar o valor `UsdInvest2`.
  - Nas segundas-feiras dentro do intervalo de datas, se AHR999 < 1.2, comprar o valor `UsdInvest1`.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - As posições são mantidas; nenhuma lógica de saída automática está incluída.
- **Stops**: Nenhum.
- **Valores padrão**:
  - UsdInvest1 = 100.
  - UsdInvest2 = 1000.
  - Length = 200.
  - Data de início = 2024-02-01, data de fim = 2025-12-31.
- **Filtros**:
  - Categoria: Acumulação.
  - Direção: Comprado.
  - Indicadores: AHR999.
  - Stops: Não.
  - Complexidade: Moderado.
  - Período: Diário.
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Médio.
