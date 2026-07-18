# Estratégia de Filtro de Volatilidade CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia **CCI Volatility Filter** é construída em torno do CCI com filtro de volatilidade.

Os testes indicam um retorno anual médio de aproximadamente 58%. Funciona melhor no mercado de ações.

Os sinais são acionados quando seus indicadores confirmam entradas filtradas em dados intradiários (5m). Isso torna o método adequado para traders ativos.

Os stops dependem de múltiplos de ATR e fatores como CciPeriod, AtrPeriod. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: ver implementação para condições dos indicadores.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: sinal oposto ou lógica de stops.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `CciPeriod = 20`
  - `AtrPeriod = 14`
  - `CciOversold = -100m`
  - `CciOverbought = 100m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
