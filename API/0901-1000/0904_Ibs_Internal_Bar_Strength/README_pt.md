# Estratégia de Força Interna de Barra IBS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

IBS Internal Bar Strength é uma estratégia de reversão à média que utiliza o fechamento da barra anterior dentro do seu range para identificar condições de sobrevendido ou sobrecomprado. Um filtro EMA opcional alinha as operações com a tendência e as entradas só são permitidas quando o preço se move um percentual mínimo a partir da última entrada. As posições são encerradas quando o IBS cruza o limiar oposto ou o tempo máximo de manutenção é atingido.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: IBS abaixo do limiar de entrada, condição EMA satisfeita e direção permitida.
  - **Vendido**: IBS acima do limiar de saída, condição EMA satisfeita e direção permitida.
- **Critérios de saída**: IBS cruzando o limiar oposto ou limite de duração da operação.
- **Stops**: Saída baseada em tempo.
- **Valores padrão**:
  - `IbsEntryThreshold` = 0.09
  - `IbsExitThreshold` = 0.985
  - `EmaPeriod` = 220
  - `MinEntryPct` = 0
  - `MaxTradeDuration` = 14
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado & Vendido
  - Indicadores: IBS, EMA
  - Complexidade: Baixo
  - Nível de risco: Médio
