# Estratégia de Oscilador Reflex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o Reflex Oscillator de John Ehlers. Entra comprado quando o oscilador cruza acima de um limiar superior e entra vendido quando cruza abaixo de um limiar inferior. As posições são fechadas quando o oscilador retorna à linha zero.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: o oscilador cruza acima de `UpperLevel`.
  - **Vendido**: o oscilador cruza abaixo de `LowerLevel`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Posição comprada: o oscilador cruza abaixo de zero.
  - Posição vendida: o oscilador cruza acima de zero.
- **Stops**: Não.
- **Valores padrão**:
  - `ReflexPeriod` = 20.
  - `SuperSmootherPeriod` = 8.
  - `PostSmoothPeriod` = 33.
  - `UpperLevel` = 1.
  - `LowerLevel` = -1.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Não
  - Complexidade: Avançado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
