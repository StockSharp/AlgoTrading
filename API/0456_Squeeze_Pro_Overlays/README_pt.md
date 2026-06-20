# Estratégia de Squeeze Pro Overlays
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Squeeze Pro Overlays detecta a contração de volatilidade quando as Bandas de Bollinger estão completamente dentro de múltiplos Canais de Keltner. Após o squeeze ser liberado, a inclinação de uma regressão linear nos preços de fechamento determina a direção da operação.

## Detalhes

- **Critérios de entrada**:
  - O squeeze termina (as Bandas de Bollinger se movem para fora do Canal de Keltner mais amplo).
  - **Comprado**: Inclinação do momentum > 0.
  - **Vendido**: Inclinação do momentum < 0.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `SqueezeLength` = 20
- **Filtros**:
  - Categoria: Rompimento de volatilidade
  - Direção: Ambos
  - Indicadores: Bollinger Bands, Keltner Channels, Linear Regression
  - Stops: Nenhum
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
