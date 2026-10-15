# Estratégia de Histograma MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Histograma MFI usa o Índice de Fluxo de Dinheiro (MFI) para detectar condições de sobrecompra e sobrevenda através de limiares configuráveis. O MFI combina preço e volume para medir a intensidade da entrada e saída de capital. Quando o indicador cruza acima do nível alto a partir de baixo, a estratégia interpreta isso como pressão de compra crescente e entra numa posição comprada enquanto fecha qualquer posição vendida existente. Inversamente, um cruzamento abaixo do nível baixo desencadeia uma entrada vendida e fecha posições compradas existentes. Os valores de stop-loss e take-profit são geridos em ticks através do mecanismo de proteção integrado.

A estratégia opera num período de velas definido pelo utilizador (4 horas por defeito) e depende de um único indicador sem filtros adicionais. Os parâmetros permitem otimizar o período do MFI, os níveis de limiar e os limites de risco, tornando o sistema adaptável a vários mercados e regimes de volatilidade.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `MFI` cruza acima de `HighLevel` a partir de baixo.
  - **Vendido**: `MFI` cruza abaixo de `LowLevel` a partir de cima.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - O sinal oposto gera uma reversão.
  - Stop-loss ou take-profit é atingido.
- **Stops**: `StopLoss` e `TakeProfit` em ticks.
- **Valores padrão**:
  - `MFI Period` = 14
  - `HighLevel` = 60
  - `LowLevel` = 40
  - `Candle Type` = 4-hour
  - `StopLoss` = 1000 ticks
  - `TakeProfit` = 2000 ticks
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Sim
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
