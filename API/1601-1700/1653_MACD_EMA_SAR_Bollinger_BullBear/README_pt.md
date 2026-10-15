# Estratégia MACD EMA SAR Bollinger BullBear
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina os indicadores MACD, cruzamento de EMA, Parabolic SAR, Bandas de Bollinger e Bulls/Bears Power. Opera apenas durante as horas ativas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: MACD < Signal, os dois últimos máximos abaixo da banda superior de Bollinger, EMA3 > EMA34, SAR abaixo do preço, Bulls Power > 0 e diminuindo.
  - **Vendido**: MACD > Signal, EMA3 < EMA34, SAR acima do preço, Bears Power < 0 e aumentando.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Sem regras de saída dedicadas; a posição fecha com o sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `MACD Fast` = 12
  - `MACD Slow` = 26
  - `MACD Signal` = 9
  - `Fast EMA Period` = 3
  - `Slow EMA Period` = 34
  - `Power Period` = 13
  - `SAR Step` = 0.02
  - `SAR Max` = 0.2
  - `Bollinger Period` = 20
  - `Bollinger Deviation` = 2.0
  - `Candle Type` = 15 minutos
  - `Session Start` = 09:00
  - `Session End` = 17:00
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
