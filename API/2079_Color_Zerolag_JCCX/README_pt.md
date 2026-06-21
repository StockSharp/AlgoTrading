# Estratégia Color Zerolag JCCX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia inspirada no indicador ColorZerolagJCCX do MetaTrader. Aproxima o oscilador original usando duas médias móveis simples.
A estratégia vai comprada quando a média rápida cruza abaixo da média lenta e vai vendida quando a média rápida cruza acima da média lenta.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `MA rápida cruza abaixo da MA lenta`
  - Vendido: `MA rápida cruza acima da MA lenta`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto
- **Stops**: `StartProtection()`
- **Valores padrão**:
  - `FastPeriod` = 8
  - `SlowPeriod` = 21
  - `CandleType` = velas de 4 horas
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Média móvel
  - Stops: Opcional
  - Complexidade: Básico
  - Período: Swing
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
