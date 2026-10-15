# Estratégia de Índice de Percentual Altista do Bitcoin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o Índice de Força Relativa (RSI) para aproximar o Índice de Percentual Altista do Bitcoin. Entra comprado quando o RSI sobe acima do nível de sobrevenda e entra vendido quando o RSI cai abaixo do nível de sobrecompra.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: RSI cruza acima do nível de sobrevenda.
  - **Vendido**: RSI cruza abaixo do nível de sobrecompra.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `RSI Period` = 14
  - `Overbought` = 70
  - `Oversold` = 30
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Baixo
  - Período: Médio prazo
