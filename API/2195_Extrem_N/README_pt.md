# Estratégia Extrem N
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Extrem N opera reversões baseadas em novas máximas e mínimas detectadas em uma janela deslizante.

A estratégia baseia-se no indicador de Canal Donchian para marcar os extremos de preço. Quando uma barra estabelece uma nova máxima relativa ao período de retrospecto e a barra seguinte estabelece uma nova mínima, uma posição comprada é aberta. Uma posição vendida é aberta quando uma nova mínima é seguida por uma nova máxima. Sinais opostos fecham as posições existentes.

- **Condições de entrada**:
  - Comprado: a barra anterior criou uma nova máxima e a barra atual criou uma nova mínima.
  - Vendido: a barra anterior criou uma nova mínima e a barra atual criou uma nova máxima.
- **Condições de saída**:
  - As posições compradas são fechadas com um sinal de entrada vendido.
  - As posições vendidas são fechadas com um sinal de entrada comprado.
- **Parâmetros**:
  - `Period` – período de retrospecto do Donchian (padrão: 9).
  - `CandleType` – período de processamento (padrão: 4 horas).
  - `BuyPosOpen` – permitir abrir posições compradas (padrão: true).
  - `SellPosOpen` – permitir abrir posições vendidas (padrão: true).
  - `BuyPosClose` – permitir fechar posições compradas (padrão: true).
  - `SellPosClose` – permitir fechar posições vendidas (padrão: true).
- **Indicadores**: Canal Donchian.
