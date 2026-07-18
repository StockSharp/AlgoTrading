# Estratégia de QQE Signals
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementa a técnica de Quantitative Qualitative Estimation sobre RSI. O indicador constrói bandas dinâmicas superiores e inferiores ao redor de uma linha RSI suavizada e rastreia os cruzamentos de banda para sinalizar mudanças de tendência. Quando RSI cruza acima da banda de rastreamento um sinal comprado é gerado; cruzamentos abaixo acionam saídas.

Ao adaptar as bandas à volatilidade, QQE busca suavizar o ruído enquanto permanece responsivo. A estratégia foca em operações compradas e depende das reversões de trades do motor para fechar posições.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: A linha RSI suavizada cruza acima da banda de rastreamento.
- **Critérios de saída**:
  - RSI cai abaixo da banda oposta ou um sinal oposto aparece.
- **Indicadores**:
  - RSI (período 14, suavização 5)
  - Bandas QQE derivadas do ATR do RSI com fator 4.238
- **Stops**: Nenhum por padrão; depende de sinais opostos.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.238
  - `Threshold` = 10
- **Filtros**:
  - Seguidor de tendência
  - Período único
  - Indicadores: RSI, QQE
  - Stops: Nenhum
  - Complexidade: Moderado
