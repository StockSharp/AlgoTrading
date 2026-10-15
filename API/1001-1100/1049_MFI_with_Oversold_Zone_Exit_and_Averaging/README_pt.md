# Estratégia MFI com Saída da Zona de Sobrevenda e Averaging
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia aguarda o Money Flow Index (MFI) entrar na zona de sobrevenda. Uma vez que o MFI sobe acima do nível de sobrevenda, coloca uma ordem de compra limitada a um percentual fixo abaixo do fechamento atual. Se a ordem não for executada dentro de um número especificado de barras, ela é cancelada. Stop-loss e take-profit são aplicados via StartProtection.

## Detalhes

- **Critérios de entrada**:
  - MFI sobe acima de `MfiOversoldLevel` após estar abaixo; coloca compra limitada `LongEntryPercentage` abaixo do fechamento.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Posição encerrada por take-profit ou stop-loss (`ExitGainPercentage`, `StopLossPercentage`).
- **Stops**: Sim, via StartProtection.
- **Valores padrão**:
  - `MfiPeriod` = 14
  - `MfiOversoldLevel` = 20
  - `LongEntryPercentage` = 0.1
  - `StopLossPercentage` = 1
  - `ExitGainPercentage` = 1
  - `CancelAfterBars` = 5
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Comprado
  - Indicadores: MFI
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
