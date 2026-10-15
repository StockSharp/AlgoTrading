# Estratégia Revolution de Bandas de Volatilidade com Sinal de Contração de Amplitude VII
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia constrói um envelope ao redor do preço usando médias móveis exponenciais e detecta quando a distância entre as bandas se contrai. Quando a contração é observada e o preço rompe acima ou abaixo das bandas suavizadas, a estratégia abre uma posição na direção do rompimento.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A amplitude está se contraindo e o preço de fechamento cruza acima da banda suavizada superior.
  - **Vendido**: A amplitude está se contraindo e o preço de fechamento cruza abaixo da banda suavizada inferior.
- **Critérios de saída**: rompimento oposto.
- **Indicadores**: envelope baseado em EMA.
- **Período**: qualquer.
