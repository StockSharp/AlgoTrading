# Estratégia de Oscilador CG Adaptativo X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Usa o Oscilador CG Adaptativo em dois períodos diferentes.
O período superior define a tendência predominante enquanto o inferior
gerencia as entradas e saídas reais baseadas em cruzamentos do oscilador.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o oscilador cruza abaixo da sua linha de sinal enquanto a tendência global está subindo
  - Vendido: o oscilador cruza acima da sua linha de sinal enquanto a tendência global está caindo
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: Sinal oposto ou indicadores de fechamento explícito
- **Stops**: Não
- **Valores padrão**:
  - `TrendAlpha` = 0.07m
  - `SignalAlpha` = 0.07m
  - `TrendCandleType` = TimeSpan.FromHours(6).TimeFrame()
  - `SignalCandleType` = TimeSpan.FromMinutes(30).TimeFrame()
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Adaptive CG Oscillator
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Multi-timeframe
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
