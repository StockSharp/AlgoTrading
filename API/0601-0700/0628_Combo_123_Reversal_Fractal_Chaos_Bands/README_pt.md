# Estratégia Combo 123 Reversal e Fractal Chaos Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia que combina um padrão de reversão 123 com rompimento de Fractal Chaos Bands.
Operações compradas ocorrem quando uma reversão 123 de alta se forma e o preço fecha acima da banda fractal superior.
Operações vendidas ocorrem quando uma reversão 123 de baixa coincide com um fechamento abaixo da banda fractal inferior.

## Detalhes

- **Critérios de entrada**:
  - Comprado: Sinal de compra do Reversal123 e fechamento acima da banda fractal superior.
  - Vendido: Sinal de venda do Reversal123 e fechamento abaixo da banda fractal inferior.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: Não
- **Valores padrão**:
  - `Length` = 15
  - `KSmoothing` = 1
  - `DLength` = 3
  - `Level` = 50m
  - `Pattern` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Padrão e rompimento
  - Direção: Ambos
  - Indicadores: Stochastic Oscillator, Fractal Chaos Bands
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
