# Estratégia de Cruzamento HMA 200 + EMA 20
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia entra comprado quando o preço está acima da Hull Moving Average de 200 períodos
e cruza acima da Exponential Moving Average de 20 períodos. Posições vendidas são
abertas quando o preço está abaixo da HMA e cruza abaixo da EMA. As posições se invertem
em sinais opostos.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Close > HMA` e `Close` cruza acima de `EMA`.
  - **Vendido**: `Close < HMA` e `Close` cruza abaixo de `EMA`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Reversão no sinal de cruzamento oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `HMA Length` = 200
  - `EMA Length` = 20
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: HMA, EMA
  - Stops: Nenhum
  - Complexidade: Simples
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
