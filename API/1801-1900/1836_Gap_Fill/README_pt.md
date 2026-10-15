# Estratégia de Preenchimento de Gaps
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Preenchimento de Gaps explora lacunas de preço entre velas consecutivas de 15 minutos.
Quando uma nova vela abre acima da máxima da vela anterior em mais de um limite configurável, a estratégia vende e coloca um limite de compra na máxima anterior, esperando que o gap seja preenchido.
Quando uma vela abre abaixo da mínima anterior em mais do limite, compra e coloca um limite de venda na mínima anterior.
O limite é calculado como `MinGapSize` passos de preço mais o spread atual entre o melhor bid e ask.

## Detalhes

- **Critérios de entrada**: Gap entre a abertura atual e a máxima/mínima anterior excede `MinGapSize` mais o spread.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**: Ordem limitada no extremo da vela anterior.
- **Stops**: Não.
- **Valores padrão**:
  - `MinGapSize` = 1
  - `Volume` = 0.1
  - `CandleType` = 15 minutos
- **Filtros**:
  - Categoria: Gap
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário (15m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
