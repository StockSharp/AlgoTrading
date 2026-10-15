# Estratégia de Oscilador Ponderado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina RSI, Money Flow Index, Williams %R e DeMarker em um oscilador ponderado suavizado por uma média móvel simples. As posições são abertas ou revertidas quando o oscilador cruza os níveis alto ou baixo configuráveis. O modo de tendência determina se as operações seguem ou vão contra os sinais do oscilador.

## Detalhes

- **Critérios de entrada**:
  - **Trend = Direct**:
    - **Comprado**: o oscilador cai abaixo do nível baixo.
    - **Vendido**: o oscilador sobe acima do nível alto.
  - **Trend = Against**:
    - **Comprado**: o oscilador sobe acima do nível alto.
    - **Vendido**: o oscilador cai abaixo do nível baixo.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: O cruzamento oposto reverte a posição.
- **Stops**: Sem stops explícitos.
- **Filtros**: Oscilador ponderado com suavização SMA.
