# Estratégia de Scalp Supertrend & CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Supertrend & CCI Scalp utiliza duas linhas Supertrend e um CCI suavizado para capturar reversões de curto prazo.
Compra quando o primeiro Supertrend está acima do preço, o segundo está abaixo do preço e o CCI suavizado está abaixo de -100. A lógica de venda espelha esta configuração.

## Detalhes

- **Critérios de entrada**: Supertrend1 acima do preço, Supertrend2 abaixo do preço, CCI suavizado < -100 (comprado); oposto para vendido
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Alinhamento oposto do Supertrend ou CCI cruzando ±100
- **Stops**: Não
- **Valores padrão**:
  - `AtrLength1` = 14
  - `Factor1` = 3
  - `AtrLength2` = 14
  - `Factor2` = 6
  - `CciLength` = 20
  - `SmoothingLength` = 5
  - `MaType` = MovingAverageTypeEnum.Simple
  - `CciLevel` = 100
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Supertrend, CCI, Moving Average
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

