# Estratégia de Momentum Squeeze de Rompimento BB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia BB Breakout Momentum Squeeze combina um oscilador de rompimento de Bollinger Bands com um filtro de squeeze de volatilidade. O squeeze é detectado quando as Bollinger Bands se movem para fora dos Keltner Channels, sinalizando uma possível expansão. Uma operação comprada ocorre quando o oscilador altista cruza acima do limiar durante essa expansão, enquanto uma operação vendida usa o cruzamento baixista. Os stops são baseados em uma banda ATR e um alvo de risco-recompensa completa a lógica de saída.

## Detalhes

- **Critérios de entrada**:
  - Squeeze desativado (Bollinger Bands fora dos Keltner Channels).
  - **Comprado**: oscilador altista cruza acima do limiar.
  - **Vendido**: oscilador baixista cruza acima do limiar.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - O preço atinge o stop ATR ou o alvo de risco-recompensa.
- **Stops**: Banda ATR com alvo de risco-recompensa.
- **Valores padrão**:
  - `BbLength` = 14
  - `BbMultiplier` = 1.0
  - `Threshold` = 50
  - `SqueezeLength` = 20
  - `SqueezeBbMultiplier` = 2.0
  - `KcMultiplier` = 2.0
  - `AtrLength` = 30
  - `AtrMultiplier` = 1.4
  - `RrRatio` = 1.5
- **Filtros**:
  - Categoria: Rompimento de volatilidade
  - Direção: Ambos
  - Indicadores: Bollinger Bands, Keltner Channels, ATR
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
