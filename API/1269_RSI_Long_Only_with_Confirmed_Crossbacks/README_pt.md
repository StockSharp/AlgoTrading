# Estratégia RSI Somente Comprado com Retornos Confirmados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aguarda que o RSI caia abaixo de um limiar e depois cruze de volta acima dele. O retorno confirma condições de sobrevenda antes de entrar em uma posição comprada. As posições fecham quando o RSI cruza acima de um nível de saída. Os parâmetros permitem operações vendidas, mas os valores padrão as desativam na prática.

## Detalhes

- **Critérios de entrada**: RSI cruza acima do nível de sobrevenda após ter estado abaixo.
- **Comprado/Vendido**: Somente comprado por padrão.
- **Critérios de saída**: RSI cruza acima do nível de saída comprado ou regras vendidas opcionais.
- **Stops**: Não.
- **Valores padrão**:
  - `CandleType` = 5 minute
  - `RsiLength` = 14
  - `Oversold` = 44
  - `LongExitLevel` = 70
  - `ShortEntryLevel` = 100
  - `ShortExitLevel` = 0
- **Filtros**:
  - Categoria: Reversão
  - Direção: Comprado
  - Indicadores: RSI
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
