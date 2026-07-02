# Estratégia Bill Williams
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Bill Williams combina o indicador Alligator com rupturas de fractais. Os maxilares, dentes e lábios devem divergir antes que uma ruptura do fractal mais recente acione uma ordem.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - Calcular máximos e mínimos fractais das últimas 5 velas.
  - A distância entre o maxilar e os dentes deve exceder `GatorDivSlowPoints`.
  - A distância entre os lábios e os dentes deve exceder `GatorDivFastPoints`.
  - **Comprado**: O preço fecha acima do último fractal de alta em pelo menos `FilterPoints` pontos e a vela é de alta.
  - **Vendido**: O preço fecha abaixo do último fractal de baixa em pelo menos `FilterPoints` pontos e a vela é de baixa.
- **Critérios de saída**:
  - Ruptura oposta.
  - Stop trailing no último fractal oposto.
- **Stops**: Stop trailing baseado em fractais.
- **Valores padrão**:
  - `FilterPoints` = 30
  - `GatorDivSlowPoints` = 250
  - `GatorDivFastPoints` = 150
  - `CandleType` = velas de 1 hora
