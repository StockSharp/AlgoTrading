# Estratégia de Cruzamento de Stochastic RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Este método converte o clássico Relative Strength Index em um Stochastic RSI, e então suaviza o resultado nas linhas %K e %D. Quando %K cruza %D dentro de zonas cuidadosamente escolhidas, o movimento implica uma mudança de curto prazo no momentum. O algoritmo só opera quando uma estrutura EMA de três camadas confirma a direção da tendência mais ampla, ajudando a filtrar falsos sinais.

Uma vez que um cruzamento aparece, o preço de fechamento também deve ficar acima ou abaixo da EMA rápida dependendo do sinal. Isso protege contra agir em oscilações que ocorrem contra a tendência prevalecente e mantém a atenção nos momentos quando o momentum se alinha com a direção. Os traders podem ajustar os períodos de suavização e o comprimento do RSI para ajustar como o sistema reage a picos de volatilidade.

O risco é referenciado por meio de uma leitura de Average True Range. Multiplicadores do ATR atual propõem stops de perda e alvos de lucro, fornecendo um nível dinâmico que se expande em mercados voláteis e se contrai quando a atividade se acalma. Embora o script não envie automaticamente ordens protetoras, esses níveis calculados auxiliam no gerenciamento manual ou podem ser vinculados a módulos de risco adicionais.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `%K` cruza acima de `%D`, `%K` em `[10,60]`, EMAs alinhadas altistamente, preço acima de EMA1.
  - **Vendido**: `%K` cruza abaixo de `%D`, `%K` em `[40,95]`, EMAs alinhadas baixistamente, preço abaixo de EMA1.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Nenhum integrado.
- **Stops**: Múltiplos de ATR sugeridos mas não colocados automaticamente.
- **Valores padrão**:
  - `SmoothK` = 3, `SmoothD` = 3.
  - `RsiLength` = 14, `StochLength` = 14.
  - `Ema1Length` = 20, `Ema2Length` = 50, `Ema3Length` = 100.
  - `AtrLength` = 14, `AtrLossMultiplier` = 1.5, `AtrProfitMultiplier` = 2.0.
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Opcional
  - Complexidade: Moderado
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Sim
  - Nível de risco: Médio
