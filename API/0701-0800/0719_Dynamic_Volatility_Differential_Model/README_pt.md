# Modelo Dinâmico de Diferencial de Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **Dynamic Volatility Differential Model (DVDM)** compara a volatilidade implícita com a volatilidade histórica. Abre comprado quando a volatilidade implícita supera a volatilidade realizada por um limiar dinâmico de desvio padrão e entra vendido quando o spread cai abaixo do limiar negativo.

Os sinais usam dados diários e não dependem de stops.

## Detalhes
- **Critérios de entrada**: Spread de volatilidade acima/abaixo dos limiares dinâmicos de desvio padrão.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Spread de volatilidade cruzando a linha zero.
- **Stops**: Não.
- **Valores padrão**:
  - `Length = 5`
  - `StdevMultiplier = 7.1m`
  - `VolatilitySecurity = "TVC:VIX"`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Volatilidade
  - Direção: Ambos
  - Indicadores: StandardDeviation
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Diário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
