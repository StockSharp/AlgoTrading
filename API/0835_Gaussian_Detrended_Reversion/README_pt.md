# Estratégia de Reversão Gaussian Detrended
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Gaussian Detrended Reversion é uma estratégia de reversão à média que usa um oscilador de preço detendenciado suavizado com uma Média Móvel Arnaud Legoux (ALMA). Posições compradas são abertas quando o oscilador suavizado cruza acima de sua versão defasada enquanto está abaixo de zero; posições vendidas são abertas em cruzamentos descendentes acima de zero. As posições são encerradas em cruzamentos opostos ou quando o oscilador cruza a linha zero.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: O DPO suavizado por ALMA cruza acima de sua linha de defasagem e está abaixo de zero.
  - **Vendido**: O DPO suavizado por ALMA cruza abaixo de sua linha de defasagem e está acima de zero.
- **Critérios de saída**: Cruzamento de defasagem oposto ou cruzamento da linha zero.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `PriceLength` = 52
  - `SmoothingLength` = 52
  - `LagLength` = 26
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado/Vendido
  - Indicadores: EMA, ALMA
  - Complexidade: Baixo
  - Nível de risco: Médio
